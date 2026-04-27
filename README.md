# Система тестирования МЧС — Web API

REST API для онлайн-тестирования сотрудников МЧС России. Поддерживает прохождение тестов, просмотр учебных материалов и отслеживание результатов.

**Стек:** .NET 10 · PostgreSQL · Dapper · JWT · Swagger

---

## Запуск

**1. База данных**

Создайте БД в PostgreSQL и примените схему:

```bash
psql -U postgres -c "CREATE DATABASE MCHSDB;"
psql -U postgres -d MCHSDB -f Database/init.sql
```

**2. Конфигурация**

Откройте `MCHSWebAPI/appsettings.json` и укажите свои параметры:

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
  },
  "Smtp": {
    "Host": "smtp.mail.ru",
    "Port": 587,
    "Username": "ВАШ_EMAIL",
    "Password": "ВАШ_ПАРОЛЬ",
    "FromName": "МЧС Система тестирования"
  }
}
```

**3. Запуск**

```bash
cd MCHSWebAPI
dotnet restore
dotnet run
```

API доступен на `http://localhost:5000`, Swagger — на `http://localhost:5000/swagger`.

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

Database/
└── init.sql           Схема и начальные данные БД
```

---

## Примеры использования

### Регистрация

```bash
curl -X POST https://localhost:5001/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"username": "user1", "password": "password123", "email": "user@example.com"}'
```

### Вход

```bash
curl -X POST https://localhost:5001/api/auth/login \
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
curl -X POST https://localhost:5001/api/testing/start/1 \
  -H "Authorization: Bearer ВАШ_ТОКЕН"

# Отправить ответы
curl -X POST https://localhost:5001/api/testing/42/answers \
  -H "Authorization: Bearer ВАШ_ТОКЕН" \
  -H "Content-Type: application/json" \
  -d '{"answers": [{"questionId": 1, "answerId": 2}, {"questionId": 2, "answerId": 6}]}'

# Завершить тест
curl -X POST https://localhost:5001/api/testing/42/finish \
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

После выполнения `init.sql` создается администратор:

- **Username:** `admin`
- **Password:** Необходимо установить

### Установка пароля

1. Сгенерируйте хеш:
```csharp
var hash = BCrypt.Net.BCrypt.HashPassword("ваш_пароль");
```

2. Обновите в БД:
```sql
UPDATE users SET password_hash = 'ХЕШ' WHERE username = 'admin';
```

---

## Тестирование

```bash
cd MCHSWebAPI.Tests
dotnet test
```

---

## Ссылки

- [Формат импорта тестов](formatPDFquestionsandanswer.txt)
- [Скрипт инициализации БД](Database/init.sql)
- [Скачать мобильное приложение к API](https://github.com/bru1f0rc3/MCHSTestMobileAPP)
