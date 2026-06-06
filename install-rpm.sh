#!/usr/bin/env bash
# Самодостаточный установщик MCHS API для RedOS (и других RHEL-совместимых).
# Запускается на целевом сервере с правами root.
#
# Схема БД, шаблон конфига и systemd-юнит встроены прямо в этот скрипт —
# никаких соседних файлов не нужно. Требуется только распакованный билд API.
#
# Параметры через переменные окружения:
#   MCHS_DB_PASSWORD   — пароль для пользователя БД mchsapi              (обязателен)
#   MCHS_JWT_KEY       — секрет для JWT, минимум 32 символа              (обязателен)
#   MCHS_API_URL       — ссылка на архив с билдом (.7z / .tar.gz / .zip).
#                        Если задана — архив скачивается и распаковывается сам.
#   MCHS_API_DIR       — каталог с распакованным билдом (где MCHSWebAPI.dll).
#                        Если не задан — ищется рядом со скриптом: ./, ./api
#   MCHS_API_PORT      — HTTP-порт API                       (по умолчанию 5000)
#   MCHS_INSTALL_DIR   — куда поставить API           (по умолчанию /opt/mchs-api)
#
# Пример (скачать билд с GitHub Release и установить одной командой):
#   sudo MCHS_DB_PASSWORD='S3cret!' MCHS_JWT_KEY='your-very-long-jwt-key-32-chars+' \
#        MCHS_API_URL='https://github.com/bru1f0rc3/MCHSTestSystemAPI/releases/download/publish/api_linux.7z' \
#        bash install-rpm.sh

set -euo pipefail

APP_USER="mchsapi"
INSTALL_DIR="${MCHS_INSTALL_DIR:-/opt/mchs-api}"
DB_NAME="mchsdb"
DB_USER="mchsapi"
DB_PASSWORD="${MCHS_DB_PASSWORD:?MCHS_DB_PASSWORD не задан}"
API_PORT="${MCHS_API_PORT:-5000}"
JWT_KEY="${MCHS_JWT_KEY:?MCHS_JWT_KEY не задан}"

log() { echo "[$(date +%H:%M:%S)] $*"; }

if [[ "$(id -u)" -ne 0 ]]; then
    echo "Скрипт должен выполняться от root." >&2
    exit 1
fi

# --- 0. Поиск / загрузка билда API --------------------------------------------
# Печатает каталог с MCHSWebAPI.dll внутри $1 (или ничего).
find_dll_dir() {
    local dll
    dll="$(find "$1" -maxdepth 6 -name MCHSWebAPI.dll -print -quit 2>/dev/null || true)"
    [[ -n "$dll" ]] && dirname "$dll"
}

API_DIR="${MCHS_API_DIR:-}"

# 0a. Каталог рядом со скриптом.
if [[ -z "$API_DIR" ]]; then
    SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" 2>/dev/null && pwd || echo "$PWD")"
    for cand in "$SCRIPT_DIR/api" "$SCRIPT_DIR" "$PWD/api" "$PWD"; do
        if [[ -f "$cand/MCHSWebAPI.dll" ]]; then API_DIR="$cand"; break; fi
    done
fi

# 0b. Если не нашли, но задан URL — качаем и распаковываем архив.
if [[ -z "$API_DIR" || ! -f "$API_DIR/MCHSWebAPI.dll" ]] && [[ -n "${MCHS_API_URL:-}" ]]; then
    log "Скачивание билда API: $MCHS_API_URL"
    DL_DIR="$(mktemp -d)"
    ARCHIVE="$DL_DIR/$(basename "${MCHS_API_URL%%\?*}")"
    EXTRACT_DIR="$DL_DIR/extracted"; mkdir -p "$EXTRACT_DIR"
    curl -fL --retry 3 "$MCHS_API_URL" -o "$ARCHIVE"
    case "$ARCHIVE" in
        *.7z)
            if ! command -v 7z >/dev/null 2>&1 && ! command -v 7za >/dev/null 2>&1; then
                log "Установка p7zip для распаковки .7z..."
                dnf install -y p7zip p7zip-plugins
            fi
            if command -v 7z >/dev/null 2>&1; then 7z x -y -o"$EXTRACT_DIR" "$ARCHIVE" >/dev/null
            else 7za x -y -o"$EXTRACT_DIR" "$ARCHIVE" >/dev/null; fi ;;
        *.tar.gz|*.tgz) tar -xzf "$ARCHIVE" -C "$EXTRACT_DIR" ;;
        *.zip) command -v unzip >/dev/null 2>&1 || dnf install -y unzip; unzip -q "$ARCHIVE" -d "$EXTRACT_DIR" ;;
        *) echo "Неизвестный формат архива: $ARCHIVE" >&2; exit 1 ;;
    esac
    API_DIR="$(find_dll_dir "$EXTRACT_DIR")"
fi

if [[ -z "$API_DIR" || ! -f "$API_DIR/MCHSWebAPI.dll" ]]; then
    echo "Не найден билд API (MCHSWebAPI.dll)." >&2
    echo "Укажите MCHS_API_URL=<ссылка на архив> или MCHS_API_DIR=/path/to/api." >&2
    exit 1
fi
log "Билд API: $API_DIR"

. /etc/os-release
log "Определён дистрибутив: $PRETTY_NAME"

# --- 1. Базовые пакеты --------------------------------------------------------
log "Установка зависимостей через dnf..."
dnf install -y \
    curl ca-certificates \
    postgresql postgresql-server postgresql-contrib \
    rsync \
    firewalld || true

# --- 2. .NET 10 ---------------------------------------------------------------
if command -v dotnet >/dev/null 2>&1 && dotnet --list-runtimes 2>/dev/null | grep -q "Microsoft.AspNetCore.App 10\."; then
    log ".NET 10 ASP.NET Core уже установлен."
else
    log "Установка .NET 10 ASP.NET Core Runtime..."
    mkdir -p /usr/share/dotnet
    curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
    chmod +x /tmp/dotnet-install.sh
    /tmp/dotnet-install.sh --channel 10.0 --runtime aspnetcore --install-dir /usr/share/dotnet
    ln -sf /usr/share/dotnet/dotnet /usr/local/bin/dotnet
    rm -f /tmp/dotnet-install.sh
fi

# --- 3. PostgreSQL: инициализация кластера ------------------------------------
PG_DATA_DIR="/var/lib/pgsql/data"
if [[ ! -f "$PG_DATA_DIR/PG_VERSION" ]]; then
    log "Инициализация кластера PostgreSQL..."
    postgresql-setup --initdb || /usr/bin/postgresql-setup initdb
fi

PG_CONF="$PG_DATA_DIR/postgresql.conf"
PG_HBA="$PG_DATA_DIR/pg_hba.conf"

if [[ ! -f "$PG_CONF" || ! -f "$PG_HBA" ]]; then
    echo "Не найдены конфиги PostgreSQL ($PG_CONF, $PG_HBA)" >&2
    exit 1
fi

# listen_addresses
if grep -qE "^[#\s]*listen_addresses" "$PG_CONF"; then
    sed -i "s|^[#\s]*listen_addresses.*|listen_addresses = '*'|" "$PG_CONF"
else
    echo "listen_addresses = '*'" >> "$PG_CONF"
fi

# Доступ по сети с паролем
if ! grep -qE "^host\s+all\s+all\s+0\.0\.0\.0/0\s+md5" "$PG_HBA"; then
    echo "host    all             all             0.0.0.0/0               md5" >> "$PG_HBA"
fi
if ! grep -qE "^host\s+all\s+all\s+::/0\s+md5" "$PG_HBA"; then
    echo "host    all             all             ::/0                    md5" >> "$PG_HBA"
fi

systemctl enable postgresql >/dev/null 2>&1 || true
systemctl restart postgresql

# --- 4. Создание роли и БД ----------------------------------------------------
log "Создание роли БД '$DB_USER' и базы '$DB_NAME'..."
sudo -u postgres psql -v ON_ERROR_STOP=1 <<SQL
DO \$\$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = '${DB_USER}') THEN
        CREATE ROLE ${DB_USER} LOGIN PASSWORD '${DB_PASSWORD}';
    ELSE
        ALTER ROLE ${DB_USER} WITH LOGIN PASSWORD '${DB_PASSWORD}';
    END IF;
END\$\$;
SQL

if ! sudo -u postgres psql -tAc "SELECT 1 FROM pg_database WHERE datname='${DB_NAME}'" | grep -q 1; then
    sudo -u postgres createdb -O "${DB_USER}" "${DB_NAME}"
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

sudo -u postgres psql -d "${DB_NAME}" -v ON_ERROR_STOP=1 -f "$INIT_SQL"
rm -f "$INIT_SQL"

# --- 5. Системный пользователь и каталог --------------------------------------
if ! id -u "$APP_USER" >/dev/null 2>&1; then
    useradd --system --no-create-home --shell /sbin/nologin "$APP_USER"
fi

mkdir -p "$INSTALL_DIR"
log "Копирование бинарников API..."
if command -v rsync >/dev/null 2>&1; then
    rsync -a --delete "$API_DIR/" "$INSTALL_DIR/"
else
    rm -rf "${INSTALL_DIR:?}/"*
    cp -a "$API_DIR/." "$INSTALL_DIR/"
fi

# --- 6. appsettings.Production.json -------------------------------------------
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

# --- 7. systemd-сервис --------------------------------------------------------
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

# --- 8. firewalld -------------------------------------------------------------
if systemctl is-active --quiet firewalld 2>/dev/null; then
    log "Открытие порта ${API_PORT}/tcp в firewalld..."
    firewall-cmd --permanent --add-port="${API_PORT}/tcp" || true
    firewall-cmd --reload || true
fi

# --- 9. SELinux ---------------------------------------------------------------
if command -v getenforce >/dev/null 2>&1 && [[ "$(getenforce)" == "Enforcing" ]]; then
    log "SELinux включён — разрешаю dotnet слушать порт ${API_PORT}."
    if command -v semanage >/dev/null 2>&1; then
        semanage port -a -t http_port_t -p tcp "${API_PORT}" 2>/dev/null \
            || semanage port -m -t http_port_t -p tcp "${API_PORT}" || true
    fi
fi

# --- 10. Проверка -------------------------------------------------------------
sleep 3
if systemctl is-active --quiet mchs-api.service; then
    log "✓ Сервис mchs-api запущен."
else
    log "✗ Сервис не запустился."
    journalctl -u mchs-api --no-pager -n 50 || true
    exit 1
fi

echo
echo "=========================================================="
echo "  MCHS API успешно развёрнут на $PRETTY_NAME"
echo "  URL:           http://<server-ip>:${API_PORT}"
echo "  Каталог:       ${INSTALL_DIR}"
echo "  БД:            ${DB_NAME} (пользователь ${DB_USER})"
echo "  Логи:          journalctl -u mchs-api -f"
echo "=========================================================="
