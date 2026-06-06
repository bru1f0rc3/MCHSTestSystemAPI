#!/usr/bin/env bash
# Самодостаточный установщик MCHS API для Ubuntu / Debian / Astra Linux.
# Запускается на целевом сервере с правами root (или через sudo).
#
# Все параметры захардкожены ниже (без переменных окружения).
# Перед установкой скрипт спрашивает: установить заново или переустановить
# (с удалением текущей службы и файлов API).
#
# Запуск:
#   sudo bash install-deb.sh
#
# Билд API берётся:
#   1) из каталога рядом со скриптом (./api или ./, где лежит MCHSWebAPI.dll);
#   2) если рядом нет — скачивается с GitHub Release (API_URL ниже).

set -euo pipefail

# ===================== ЖЁСТКО ЗАДАННЫЕ ПАРАМЕТРЫ =====================
APP_USER="mchsapi"
INSTALL_DIR="/opt/mchs-api"
DB_NAME="MCHSDB"
DB_USER="postgres"
DB_PASSWORD="123123"
API_PORT="5000"
JWT_KEY="YourSuperSecretKeyForJWTTokenGenerationMustBeAtLeast32Characters!"
API_URL="https://github.com/bru1f0rc3/MCHSTestSystemAPI/releases/download/publish/api_linux.7z"
# ====================================================================

log() { echo "[$(date +%H:%M:%S)] $*"; }

if [[ "$(id -u)" -ne 0 ]]; then
    echo "Скрипт должен выполняться от root (используйте sudo)." >&2
    exit 1
fi

# --- Выбор действия: установить / переустановить ------------------------------
SERVICE_EXISTS=0
if systemctl list-unit-files 2>/dev/null | grep -q '^mchs-api\.service' || [[ -d "$INSTALL_DIR" ]]; then
    SERVICE_EXISTS=1
fi

ACTION="install"
if [[ "$SERVICE_EXISTS" -eq 1 ]]; then
    echo
    echo "Обнаружена существующая установка MCHS API."
    echo "  1) Переустановить — удалить текущую службу и API, поставить заново (по умолчанию)"
    echo "  2) Установить поверх — обновить файлы, не удаляя службу заранее"
    echo "  3) Отмена"
    CHOICE=""
    read -r -p "Выберите действие [1/2/3]: " CHOICE </dev/tty || CHOICE=""
    case "$CHOICE" in
        2) ACTION="install" ;;
        3) echo "Отменено пользователем."; exit 0 ;;
        *) ACTION="reinstall" ;;
    esac
fi

# --- Удаление текущей установки (только при переустановке) --------------------
if [[ "$ACTION" == "reinstall" ]]; then
    log "Переустановка: удаляю текущую службу и файлы API..."
    systemctl stop mchs-api.service 2>/dev/null || true
    systemctl disable mchs-api.service 2>/dev/null || true
    rm -f /etc/systemd/system/mchs-api.service
    systemctl daemon-reload 2>/dev/null || true
    rm -rf "${INSTALL_DIR:?}"
    log "Старая установка удалена."
fi

# --- 0. Поиск / загрузка билда API --------------------------------------------
# Печатает каталог с MCHSWebAPI.dll внутри $1 (или ничего).
find_dll_dir() {
    local dll
    dll="$(find "$1" -maxdepth 6 -name MCHSWebAPI.dll -print -quit 2>/dev/null || true)"
    [[ -n "$dll" ]] && dirname "$dll"
}

API_DIR=""

# 0a. Каталог рядом со скриптом.
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" 2>/dev/null && pwd || echo "$PWD")"
for cand in "$SCRIPT_DIR/api" "$SCRIPT_DIR" "$PWD/api" "$PWD"; do
    if [[ -f "$cand/MCHSWebAPI.dll" ]]; then API_DIR="$cand"; break; fi
done

# 0b. Если рядом не нашли — качаем и распаковываем архив с GitHub Release.
if [[ -z "$API_DIR" || ! -f "$API_DIR/MCHSWebAPI.dll" ]]; then
    log "Билд рядом не найден — скачиваю с GitHub Release: $API_URL"
    DL_DIR="$(mktemp -d)"
    ARCHIVE="$DL_DIR/$(basename "${API_URL%%\?*}")"
    EXTRACT_DIR="$DL_DIR/extracted"; mkdir -p "$EXTRACT_DIR"
    curl -fL --retry 3 "$API_URL" -o "$ARCHIVE"
    case "$ARCHIVE" in
        *.7z)
            if ! command -v 7z >/dev/null 2>&1 && ! command -v 7za >/dev/null 2>&1; then
                log "Установка p7zip для распаковки .7z..."
                apt-get update -y && apt-get install -y p7zip-full
            fi
            if command -v 7z >/dev/null 2>&1; then 7z x -y -o"$EXTRACT_DIR" "$ARCHIVE" >/dev/null
            else 7za x -y -o"$EXTRACT_DIR" "$ARCHIVE" >/dev/null; fi ;;
        *.tar.gz|*.tgz) tar -xzf "$ARCHIVE" -C "$EXTRACT_DIR" ;;
        *.zip) command -v unzip >/dev/null 2>&1 || apt-get install -y unzip; unzip -q "$ARCHIVE" -d "$EXTRACT_DIR" ;;
        *) echo "Неизвестный формат архива: $ARCHIVE" >&2; exit 1 ;;
    esac
    API_DIR="$(find_dll_dir "$EXTRACT_DIR")"
fi

if [[ -z "$API_DIR" || ! -f "$API_DIR/MCHSWebAPI.dll" ]]; then
    echo "Не найден билд API (MCHSWebAPI.dll)." >&2
    echo "Положите распакованный билд рядом со скриптом (в ./api) или проверьте API_URL." >&2
    exit 1
fi
log "Билд API: $API_DIR"

# --- 1. Определение дистрибутива ----------------------------------------------
. /etc/os-release
DISTRO_ID="${ID,,}"
log "Определён дистрибутив: $PRETTY_NAME"

# Astra Linux маскируется под Debian — обрабатываем как Debian.
case "$DISTRO_ID" in
    ubuntu|debian|astra) ;;
    *)
        log "Внимание: дистрибутив '$DISTRO_ID' не из списка протестированных, продолжаю как Debian-совместимый."
        ;;
esac

export DEBIAN_FRONTEND=noninteractive

# --- 2. Базовые пакеты --------------------------------------------------------
log "Обновление списков пакетов и установка зависимостей..."
apt-get update -y
apt-get install -y --no-install-recommends \
    curl ca-certificates gnupg lsb-release rsync \
    postgresql postgresql-contrib \
    ufw || true

# --- 3. .NET 10 (ASP.NET Core Runtime) ----------------------------------------
if command -v dotnet >/dev/null 2>&1 && dotnet --list-runtimes 2>/dev/null | grep -q "Microsoft.AspNetCore.App 10\."; then
    log ".NET 10 ASP.NET Core уже установлен."
else
    log "Установка .NET 10 ASP.NET Core Runtime через dotnet-install.sh..."
    mkdir -p /usr/share/dotnet
    curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
    chmod +x /tmp/dotnet-install.sh
    /tmp/dotnet-install.sh --channel 10.0 --runtime aspnetcore --install-dir /usr/share/dotnet
    ln -sf /usr/share/dotnet/dotnet /usr/local/bin/dotnet
    rm -f /tmp/dotnet-install.sh
fi

# --- 4. Настройка PostgreSQL --------------------------------------------------
log "Настройка PostgreSQL для прослушивания всех адресов..."

PG_VERSION="$(ls /etc/postgresql 2>/dev/null | sort -V | tail -n1 || true)"
if [[ -z "${PG_VERSION:-}" ]]; then
    log "Не удалось определить версию PostgreSQL в /etc/postgresql — ищу конфиг иначе."
    PG_CONF="$(find /etc -name postgresql.conf -path '*/postgresql/*' 2>/dev/null | head -n1)"
    PG_HBA="$(find /etc -name pg_hba.conf -path '*/postgresql/*' 2>/dev/null | head -n1)"
else
    PG_CONF="/etc/postgresql/${PG_VERSION}/main/postgresql.conf"
    PG_HBA="/etc/postgresql/${PG_VERSION}/main/pg_hba.conf"
fi

if [[ ! -f "$PG_CONF" || ! -f "$PG_HBA" ]]; then
    echo "Не найдены конфиги PostgreSQL ($PG_CONF, $PG_HBA)" >&2
    exit 1
fi

# listen_addresses = '*'
if grep -qE "^[#\s]*listen_addresses" "$PG_CONF"; then
    sed -i "s|^[#\s]*listen_addresses.*|listen_addresses = '*'|" "$PG_CONF"
else
    echo "listen_addresses = '*'" >> "$PG_CONF"
fi

# host all all 0.0.0.0/0 md5 (если ещё нет такой строки)
if ! grep -qE "^host\s+all\s+all\s+0\.0\.0\.0/0\s+md5" "$PG_HBA"; then
    echo "host    all             all             0.0.0.0/0               md5" >> "$PG_HBA"
fi
if ! grep -qE "^host\s+all\s+all\s+::/0\s+md5" "$PG_HBA"; then
    echo "host    all             all             ::/0                    md5" >> "$PG_HBA"
fi

systemctl enable postgresql >/dev/null 2>&1 || true
systemctl restart postgresql

# --- 5. Пароль пользователя postgres и создание БД ----------------------------
# Переходим в каталог, доступный пользователю postgres, чтобы psql не сыпал
# предупреждениями «could not change directory to "/root"».
cd /tmp
log "Установка пароля пользователя '$DB_USER' и создание базы '$DB_NAME'..."
sudo -u postgres psql -v ON_ERROR_STOP=1 <<SQL
ALTER USER ${DB_USER} WITH PASSWORD '${DB_PASSWORD}';
SQL

if ! sudo -u postgres psql -tAc "SELECT 1 FROM pg_database WHERE datname='${DB_NAME}'" | grep -q 1; then
    sudo -u postgres psql -v ON_ERROR_STOP=1 -c "CREATE DATABASE \"${DB_NAME}\" OWNER ${DB_USER};"
fi

log "Инициализация схемы БД..."
INIT_SQL="$(mktemp)"
cat > "$INIT_SQL" <<'INITSQL'
DROP TABLE IF EXISTS cheat_events CASCADE;
DROP TABLE IF EXISTS user_answers CASCADE;
DROP TABLE IF EXISTS test_results CASCADE;
DROP TABLE IF EXISTS answers CASCADE;
DROP TABLE IF EXISTS questions CASCADE;
DROP TABLE IF EXISTS tests CASCADE;
DROP TABLE IF EXISTS reports CASCADE;
DROP TABLE IF EXISTS lectures CASCADE;
DROP TABLE IF EXISTS paths CASCADE;
DROP TABLE IF EXISTS users CASCADE;
DROP TABLE IF EXISTS roles CASCADE;

DROP VIEW IF EXISTS v_test_results_details CASCADE;
DROP VIEW IF EXISTS v_test_statistics CASCADE;
DROP FUNCTION IF EXISTS calculate_test_score(INT) CASCADE;

CREATE TABLE roles (
    id SERIAL PRIMARY KEY,
    name VARCHAR(50) NOT NULL UNIQUE
);

INSERT INTO roles (name) VALUES ('superadmin'), ('admin'), ('guest'), ('user');

CREATE TABLE paths (
    id SERIAL PRIMARY KEY,
    video_path TEXT,
    document_path TEXT
);

CREATE TABLE users (
    id SERIAL PRIMARY KEY,
    username VARCHAR(100) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    role_id INT NOT NULL REFERENCES roles(id) ON DELETE RESTRICT,
    device_id VARCHAR(255) UNIQUE,
    email VARCHAR(255),
    last_name  VARCHAR(100),
    first_name VARCHAR(100),
    patronymic VARCHAR(100),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_users_username ON users(username);
CREATE INDEX idx_users_role_id ON users(role_id);
CREATE UNIQUE INDEX uq_users_email_not_null
    ON users (email)
    WHERE email IS NOT NULL;

CREATE TABLE lectures (
    id SERIAL PRIMARY KEY,
    title VARCHAR(255) NOT NULL,
    text_content TEXT,
    path_id INT REFERENCES paths(id) ON DELETE SET NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_lectures_path_id ON lectures(path_id);

CREATE TABLE tests (
    id SERIAL PRIMARY KEY,
    lecture_id INT REFERENCES lectures(id) ON DELETE SET NULL,
    title VARCHAR(255) NOT NULL,
    description TEXT,
    time_limit_minutes INT,
    passing_score INT NOT NULL DEFAULT 70,
    created_by INT NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_tests_lecture_id ON tests(lecture_id);
CREATE INDEX idx_tests_created_by ON tests(created_by);

CREATE TABLE questions (
    id SERIAL PRIMARY KEY,
    test_id INT NOT NULL REFERENCES tests(id) ON DELETE CASCADE,
    question_text TEXT NOT NULL,
    position INT NOT NULL DEFAULT 0
);

CREATE INDEX idx_questions_test_id ON questions(test_id);

CREATE TABLE answers (
    id SERIAL PRIMARY KEY,
    question_id INT NOT NULL REFERENCES questions(id) ON DELETE CASCADE,
    answer_text TEXT NOT NULL,
    is_correct BOOLEAN NOT NULL DEFAULT FALSE,
    position INT NOT NULL DEFAULT 0
);

CREATE INDEX idx_answers_question_id ON answers(question_id);

CREATE TABLE test_results (
    id SERIAL PRIMARY KEY,
    user_id INT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    test_id INT NOT NULL REFERENCES tests(id) ON DELETE CASCADE,
    started_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    finished_at TIMESTAMP,
    score FLOAT8,
    cheat_attempts INT NOT NULL DEFAULT 0,
    auto_submitted BOOLEAN NOT NULL DEFAULT FALSE
);

CREATE INDEX idx_test_results_user_id ON test_results(user_id);
CREATE INDEX idx_test_results_test_id ON test_results(test_id);

CREATE TABLE user_answers (
    id SERIAL PRIMARY KEY,
    test_result_id INT NOT NULL REFERENCES test_results(id) ON DELETE CASCADE,
    question_id INT NOT NULL REFERENCES questions(id) ON DELETE CASCADE,
    answer_id INT REFERENCES answers(id) ON DELETE SET NULL,
    answered_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_user_answers_test_result_id ON user_answers(test_result_id);
CREATE INDEX idx_user_answers_question_id ON user_answers(question_id);

CREATE TABLE cheat_events (
    id SERIAL PRIMARY KEY,
    test_result_id INT NOT NULL REFERENCES test_results(id) ON DELETE CASCADE,
    event_type VARCHAR(50) NOT NULL,
    details TEXT,
    occurred_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_cheat_events_test_result_id ON cheat_events(test_result_id);

CREATE TABLE reports (
    id SERIAL PRIMARY KEY,
    created_by INT NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    report_date DATE NOT NULL DEFAULT CURRENT_DATE,
    content JSONB,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_reports_created_by ON reports(created_by);
CREATE INDEX idx_reports_report_date ON reports(report_date);

CREATE OR REPLACE VIEW v_test_results_details AS
SELECT
    tr.id as result_id,
    u.username,
    t.title as test_title,
    l.title as lecture_title,
    tr.started_at,
    tr.finished_at,
    tr.score,
    tr.cheat_attempts,
    tr.auto_submitted,
    CASE
        WHEN tr.finished_at IS NULL THEN 'in_progress'
        WHEN tr.score >= t.passing_score THEN 'passed'
        ELSE 'failed'
    END as status
FROM test_results tr
JOIN users u ON tr.user_id = u.id
JOIN tests t ON tr.test_id = t.id
LEFT JOIN lectures l ON t.lecture_id = l.id;

CREATE OR REPLACE VIEW v_test_statistics AS
SELECT
    t.id as test_id,
    t.title,
    COUNT(tr.id) as total_attempts,
    COUNT(CASE WHEN tr.finished_at IS NOT NULL THEN 1 END) as completed_attempts,
    AVG(tr.score) as average_score,
    MIN(tr.score) as min_score,
    MAX(tr.score) as max_score
FROM tests t
LEFT JOIN test_results tr ON t.id = tr.test_id
GROUP BY t.id, t.title;

CREATE OR REPLACE FUNCTION calculate_test_score(p_test_result_id INT)
RETURNS FLOAT8 AS $$
DECLARE
    v_total_questions INT;
    v_correct_answers INT;
    v_score FLOAT8;
BEGIN
    SELECT COUNT(DISTINCT q.id) INTO v_total_questions
    FROM test_results tr
    JOIN tests t ON tr.test_id = t.id
    JOIN questions q ON t.id = q.test_id
    WHERE tr.id = p_test_result_id;

    SELECT COUNT(*) INTO v_correct_answers
    FROM user_answers ua
    JOIN answers a ON ua.answer_id = a.id
    WHERE ua.test_result_id = p_test_result_id AND a.is_correct = TRUE;

    IF v_total_questions > 0 THEN
        v_score := (v_correct_answers::FLOAT8 / v_total_questions::FLOAT8) * 100;
    ELSE
        v_score := 0;
    END IF;

    UPDATE test_results
    SET score = v_score, finished_at = CURRENT_TIMESTAMP
    WHERE id = p_test_result_id;

    RETURN v_score;
END;
$$ LANGUAGE plpgsql;
INITSQL

# SQL подаётся через stdin (его открывает root-шелл): иначе пользователь postgres
# не смог бы прочитать временный файл, созданный root с правами 600.
sudo -u postgres psql -d "${DB_NAME}" -v ON_ERROR_STOP=1 -f - < "$INIT_SQL"
rm -f "$INIT_SQL"

# --- 6. Системный пользователь и каталог --------------------------------------
log "Создание системного пользователя $APP_USER..."
if ! id -u "$APP_USER" >/dev/null 2>&1; then
    useradd --system --no-create-home --shell /usr/sbin/nologin "$APP_USER"
fi

mkdir -p "$INSTALL_DIR"
log "Копирование бинарников API в $INSTALL_DIR..."
if command -v rsync >/dev/null 2>&1; then
    rsync -a --delete "$API_DIR/" "$INSTALL_DIR/"
else
    rm -rf "${INSTALL_DIR:?}/"*
    cp -a "$API_DIR/." "$INSTALL_DIR/"
fi

# --- 7. Конфигурация приложения -----------------------------------------------
log "Генерация appsettings.Production.json..."
CONN_STR="Host=127.0.0.1;Port=5432;Database=${DB_NAME};Username=${DB_USER};Password=${DB_PASSWORD};Include Error Detail=true"

# Экранирование значений для JSON (обратный слэш и кавычка).
json_escape() {
    local s=$1
    s=${s//\\/\\\\}
    s=${s//\"/\\\"}
    printf '%s' "$s"
}
CONN_STR_JSON="$(json_escape "$CONN_STR")"
JWT_KEY_JSON="$(json_escape "$JWT_KEY")"

cat > "$INSTALL_DIR/appsettings.Production.json" <<EOF
{
  "ConnectionStrings": {
    "DefaultConnection": "${CONN_STR_JSON}"
  },
  "Jwt": {
    "Key": "${JWT_KEY_JSON}",
    "Issuer": "MCHSWebAPI",
    "Audience": "MCHSMobileApp",
    "ExpirationHours": 24
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://0.0.0.0:${API_PORT}"
      }
    }
  },
  "AllowedHosts": "*"
}
EOF

chown -R "$APP_USER:$APP_USER" "$INSTALL_DIR"
chmod 750 "$INSTALL_DIR"
chmod 640 "$INSTALL_DIR/appsettings.Production.json"

# --- 8. systemd-сервис --------------------------------------------------------
log "Регистрация systemd-сервиса mchs-api.service..."
cat > /etc/systemd/system/mchs-api.service <<EOF
[Unit]
Description=MCHS Test System API
After=network.target postgresql.service
Wants=postgresql.service

[Service]
Type=simple
WorkingDirectory=${INSTALL_DIR}
ExecStart=/usr/bin/dotnet ${INSTALL_DIR}/MCHSWebAPI.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=mchs-api
User=${APP_USER}
Group=${APP_USER}

Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://*:${API_PORT}
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false
Environment=DOTNET_NOLOGO=true

# Ограничения по безопасности
NoNewPrivileges=true
ProtectSystem=strict
ProtectHome=true
PrivateTmp=true
ReadWritePaths=${INSTALL_DIR}

[Install]
WantedBy=multi-user.target
EOF

# dotnet может лежать не в /usr/bin — подставим реальный путь, если найдём.
DOTNET_BIN="$(command -v dotnet || true)"
if [[ -n "$DOTNET_BIN" && "$DOTNET_BIN" != "/usr/bin/dotnet" ]]; then
    sed -i "s|/usr/bin/dotnet|${DOTNET_BIN}|g" /etc/systemd/system/mchs-api.service
fi

systemctl daemon-reload
systemctl enable mchs-api.service
systemctl restart mchs-api.service

# --- 9. Брандмауэр ------------------------------------------------------------
if command -v ufw >/dev/null 2>&1; then
    log "Открытие порта ${API_PORT}/tcp в ufw..."
    ufw allow "${API_PORT}/tcp" || true
fi

# --- 10. Проверка -------------------------------------------------------------
sleep 3
if systemctl is-active --quiet mchs-api.service; then
    log "✓ Сервис mchs-api запущен."
else
    log "✗ Сервис mchs-api не запустился. journalctl -u mchs-api -n 100"
    journalctl -u mchs-api --no-pager -n 50 || true
    exit 1
fi

echo
echo "=========================================================="
echo "  MCHS API успешно развёрнут"
echo "  URL:           http://<server-ip>:${API_PORT}"
echo "  Каталог:       ${INSTALL_DIR}"
echo "  БД:            ${DB_NAME} (пользователь ${DB_USER})"
echo "  Логи:          journalctl -u mchs-api -f"
echo "  Управление:    systemctl {start|stop|restart|status} mchs-api"
echo "=========================================================="
