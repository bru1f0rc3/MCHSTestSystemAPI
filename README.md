# Система тестирования МЧС — Web API

REST API для онлайн-тестирования сотрудников МЧС России. Поддерживает прохождение тестов, просмотр учебных материалов и отслеживание результатов.

**Стек:** .NET 10 · PostgreSQL · Dapper · JWT · Scalar (OpenAPI)

---

## Установка на сервер (VPS)

Установочные скрипты `install-deb.sh` / `install-rpm.sh` **самодостаточны**: ставят
**.NET 10** и **PostgreSQL**, настраивают БД, накатывают схему, генерируют конфиг,
регистрируют systemd-сервис `mchs-api` и открывают порт в брандмауэре.

Все параметры (БД, JWT-ключ, порт, каталог) **захардкожены в начале скрипта** —
переменные окружения указывать не нужно. При необходимости поменяйте значения в блоке
`ЖЁСТКО ЗАДАННЫЕ ПАРАМЕТРЫ` вверху файла.

Перед установкой скрипт **спрашивает действие**, если установка уже есть:

```
1) Переустановить — удалить текущую службу и API, поставить заново (по умолчанию)
2) Установить поверх — обновить файлы
3) Отмена
```

Скрипт сам скачивает билд `api_linux.7z` с GitHub Release (ссылка `API_URL` зашита
в его заголовке), поэтому ничего качать и распаковывать вручную не нужно.

### Установка — одной командой

Зайдите на VPS по SSH под root (или через `sudo`) и выполните **один** блок ниже.
Скрипт всё сделает сам; если установка уже есть — задаст вопрос (по умолчанию переустановка).

**Ubuntu / Debian / Astra Linux**

```bash
curl -fsSL https://github.com/bru1f0rc3/MCHSTestSystemAPI/releases/download/publish/install-deb.sh -o install.sh
sudo bash install.sh
```

**RedOS / RHEL-совместимые**

```bash
curl -fsSL https://github.com/bru1f0rc3/MCHSTestSystemAPI/releases/download/publish/install-rpm.sh -o install.sh
sudo bash install.sh
```

> Чтобы выкатилась актуальная версия кода, в GitHub Release (тег `publish`) должен лежать
> свежий `api_linux.7z` — см. [Обновление билда](#обновление-билда).

**Параметры по умолчанию (в начале скрипта):**

| Параметр | Значение по умолчанию | Описание |
|----------|----------------------|----------|
| `DB_NAME` | `MCHSDB` | Имя базы данных |
| `DB_USER` | `postgres` | Пользователь БД |
| `DB_PASSWORD` | `123123` | Пароль пользователя БД |
| `API_PORT` | `5000` | HTTP-порт API |
| `INSTALL_DIR` | `/opt/mchs-api` | Каталог установки |
| `JWT_KEY` | демо-ключ (≥32 симв.) | Секрет для подписи JWT |
| `API_URL` | ссылка на GitHub Release | Откуда скачать билд, если нет `./api` |

> **Безопасность:** значения по умолчанию (`123123`, демо JWT-ключ, доступ к Postgres
> с `0.0.0.0/0`) удобны для теста, но для боевого сервера их нужно заменить на свои.

После установки API доступен на `http://<ip-сервера>:5000`, документация (Scalar) — на `http://<ip-сервера>:5000/scalar/v1`.

**Управление сервисом:**

```bash
systemctl status mchs-api      # статус
systemctl restart mchs-api     # перезапуск
journalctl -u mchs-api -f      # логи в реальном времени
```

> **Важно:** при первом старте создаются учётки по умолчанию (см. [Учётные данные по умолчанию](#учетные-данные-по-умолчанию)). Сразу смените пароли.

### Обновление билда

Чтобы curl-команда ставила актуальный код, нужно один раз пересобрать архив и заменить
его в GitHub Release (тег `publish`). Делается на машине разработчика:

```bash
cd MCHSWebAPI
dotnet publish -c Release -r linux-x64 --self-contained false -o ../publish/api
7z a ../publish/api_linux.7z ../publish/api/*     # или: tar -czf api_linux.tar.gz -C ../publish/api .
```

Затем загрузите получившийся `api_linux.7z` в Release (заменив старый) — например через `gh`:

```bash
gh release upload publish ../publish/api_linux.7z --clobber
```

После этого на сервере достаточно снова запустить `sudo bash install.sh` и выбрать
переустановку — скрипт скачает свежий архив сам.

---

## Локальный запуск (для разработки)

**1. PostgreSQL** — поднимите локально и создайте пустую БД:

```bash
psql -U postgres -c "CREATE DATABASE MCHSDB;"
```

Схему создавать вручную не нужно — приложение само накатывает её и сидит дефолтных пользователей при старте.

**2. Конфигурация** — `MCHSWebAPI/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=MCHSDB;Username=postgres;Password=ВАШ_ПАРОЛЬ"
  },
  "Jwt": {
    "Key": "СЕКРЕТНЫЙ_КЛЮЧ_МИНИМУМ_32_СИМВОЛА",
    "Issuer": "MCHSWebAPI",
    "Audience": "MCHSMobileApp",
    "ExpirationHours": 24
  }
}
```

**3. Запуск:**

```bash
cd MCHSWebAPI
dotnet restore
dotnet run
```

API доступен на `http://localhost:5000`, документация (Scalar) — на `http://localhost:5000/scalar/v1`.

---

## Импорт тестов

Тесты можно загружать через текстовый файл (см. [`formatPDFquestionsandanswer.txt`](formatPDFquestionsandanswer.txt)):

```
Название теста
1. Текст вопроса?
а) Вариант 1
б) Правильный вариант [true]
в) Вариант 3
г) Вариант 4

2. Следующий вопрос?
...
```

- Первая строка — название теста
- Вопросы нумеруются: `1.`, `2.`, ...
- Ответы обозначаются: `а)`, `б)`, `в)`, `г)`
- Правильный ответ помечается `[true]`

---

## API Эндпоинты

### Аутентификация `/api/auth`

| Метод | Endpoint | Описание | Доступ |
|-------|----------|----------|--------|
| POST | `/login` | Вход в систему | Публичный |
| POST | `/register` | Регистрация | Публичный |
| POST | `/guest` | Гостевой аккаунт | Публичный |
| POST | `/change-password` | Смена пароля | Авторизован |
| GET | `/me` | Текущий пользователь | Авторизован |

### Пользователи `/api/users`

Управление пользователями (только админ).

| Метод | Endpoint | Описание |
|-------|----------|----------|
| GET | `/` | Список пользователей |
| GET | `/{id}` | Информация о пользователе |
| POST | `/` | Создать пользователя |
| PUT | `/{id}` | Обновить пользователя |
| DELETE | `/{id}` | Удалить пользователя |

### Лекции `/api/lectures`

| Метод | Endpoint | Описание | Доступ |
|-------|----------|----------|--------|
| GET | `/` | Список лекций | Авторизован |
| GET | `/{id}` | Получить лекцию | Авторизован |
| POST | `/` | Создать лекцию | Админ |
| PUT | `/{id}` | Обновить лекцию | Админ |
| DELETE | `/{id}` | Удалить лекцию | Админ |

### Тесты `/api/tests`

| Метод | Endpoint | Описание | Доступ |
|-------|----------|----------|--------|
| GET | `/` | Список тестов | Авторизован |
| GET | `/{id}` | Информация о тесте | Авторизован |
| GET | `/{id}/full` | Тест с вопросами | Авторизован |
| GET | `/by-lecture/{lectureId}` | Тесты по лекции | Авторизован |
| POST | `/` | Создать тест | Админ |
| PUT | `/{id}` | Обновить тест | Админ |
| DELETE | `/{id}` | Удалить тест | Админ |
| POST | `/{testId}/questions` | Добавить вопрос | Админ |
| PUT | `/questions/{id}` | Обновить вопрос | Админ |
| DELETE | `/questions/{id}` | Удалить вопрос | Админ |
| POST | `/questions/{id}/answers` | Добавить ответ | Админ |
| PUT | `/answers/{id}` | Обновить ответ | Админ |
| DELETE | `/answers/{id}` | Удалить ответ | Админ |

### Прохождение тестов `/api/testing`

| Метод | Endpoint | Описание | Доступ |
|-------|----------|----------|--------|
| POST | `/start/{testId}` | Начать тест | Авторизован |
| POST | `/{testResultId}/answer` | Отправить ответ | Авторизован |
| POST | `/{testResultId}/answers` | Отправить ответы | Авторизован |
| POST | `/{testResultId}/finish` | Завершить тест | Авторизован |
| GET | `/result/{testResultId}` | Результат теста | Авторизован |
| GET | `/result/{testResultId}/detail` | Детальный результат | Авторизован |
| GET | `/my-results` | История результатов | Авторизован |
| GET | `/all-results` | Все результаты | Админ |

### Отчеты `/api/reports`

Только для администраторов.

| Метод | Endpoint | Описание |
|-------|----------|----------|
| GET | `/dashboard` | Статистика системы |
| GET | `/test-statistics` | Статистика по тестам |
| GET | `/` | Список отчетов |
| GET | `/{id}` | Получить отчет |
| POST | `/` | Создать отчет |
| DELETE | `/{id}` | Удалить отчет |

---

## Структура проекта

```
MCHSWebAPI/
├── Controllers/       API контроллеры
├── Data/              Подключение к БД и инициализация
├── DTOs/              Объекты для запросов и ответов
├── Models/            Модели данных
├── Services/          Бизнес-логика
├── Properties/        Настройки запуска
├── appsettings.json   Конфигурация
└── Program.cs         Точка входа

MCHSWebAPI.Tests/
├── Controllers/       Тесты контроллеров
├── Services/          Тесты сервисов
└── Helpers/           Вспомогательные фабрики данных

install-deb.sh         Установщик для Ubuntu / Debian / Astra Linux
install-rpm.sh         Установщик для RedOS / RHEL-совместимых
```

> Схема БД и шаблон конфига встроены прямо в установочные скрипты — отдельной папки `Database/` больше нет. Для локальной разработки схему создаёт само приложение при старте.

---

## Примеры использования

### Регистрация

```bash
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"username": "user1", "password": "password123", "lastName": "Иванов", "firstName": "Иван"}'
```

### Вход

```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username": "user1", "password": "password123"}'
```

**Ответ:**
```json
{
  "userId": 1,
  "username": "user1",
  "role": "user",
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "expiresAt": "2026-01-13T10:00:00Z"
}
```

### Прохождение теста

```bash
# Начать тест
curl -X POST http://localhost:5000/api/testing/start/1 \
  -H "Authorization: Bearer ВАШ_ТОКЕН"

# Отправить ответы
curl -X POST http://localhost:5000/api/testing/42/answers \
  -H "Authorization: Bearer ВАШ_ТОКЕН" \
  -H "Content-Type: application/json" \
  -d '{"answers": [{"questionId": 1, "answerId": 2}, {"questionId": 2, "answerId": 6}]}'

# Завершить тест
curl -X POST http://localhost:5000/api/testing/42/finish \
  -H "Authorization: Bearer ВАШ_ТОКЕН"
```

---

## Роли пользователей

| Роль | Описание | Возможности |
|------|----------|-------------|
| **admin** | Администратор | Полный доступ: управление тестами, лекциями, пользователями, просмотр статистики и отчетов |
| **user** | Пользователь | Прохождение тестов, просмотр лекций, просмотр своих результатов |
| **guest** | Гость | Ограниченный доступ для быстрого входа по device_id |

При регистрации пользователь получает роль **user**. Гостевой аккаунт можно конвертировать в обычный при регистрации с тем же device_id.

---

## Учетные данные по умолчанию

При первом старте приложение создаёт двух пользователей:

| Роль | Username | Password |
|------|----------|----------|
| **superadmin** | `superadmin` | `superadmin123` |
| **admin** | `admin` | `admin123` |

> **Обязательно смените пароли** после первого входа — через мобильное приложение (Профиль → Личные данные) или эндпоинт `POST /api/auth/change-password`.

---

## Тестирование

```bash
cd MCHSWebAPI.Tests
dotnet test
```

---

## Ссылки

- [Релизы и установочные скрипты](https://github.com/bru1f0rc3/MCHSTestSystemAPI/releases/tag/publish)
- [Формат импорта тестов](formatPDFquestionsandanswer.txt)
- [Скачать мобильное приложение к API](https://github.com/bru1f0rc3/MCHSTestMobileAPP)
