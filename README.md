# MCHS Project API

Мобильное приложение для обучения и тестирования сотрудников МЧС. Backend API разработан на ASP.NET Core с использованием PostgreSQL.

## 🚀 Технологии

- **.NET 10.0** - Современный фреймворк для создания веб-приложений
- **PostgreSQL** - Надежная реляционная база данных
- **Dapper** - Легковесный ORM для работы с БД
- **JWT Bearer** - Аутентификация и авторизация
- **BCrypt.Net** - Хеширование паролей
- **Swagger/OpenAPI** - Документация API
- **iText7** - Парсинг PDF документов
- **DocumentFormat.OpenXml** - Парсинг DOCX документов

## 📋 Функциональность

### 🔐 Аутентификация
- Регистрация пользователей с хешированием паролей (BCrypt)
- Вход через JWT токены
- Refresh Token для продления сессии
- Смена пароля
- Выход из системы

### 📚 Управление контентом
- **Лекции**: создание, редактирование, удаление учебных материалов
- **Тесты**: привязка к лекциям с автоматической генерацией из документов
- **Вопросы и ответы**: множественный выбор с указанием правильных ответов
- **Медиа файлы**: видео и документы для лекций

### 📄 Автоматическая генерация тестов
Загрузка PDF/DOCX документов с автоматическим распознаванием:
- Вопросов (паттерн: `Вопрос 1:`, `Question 1:`)
- Вариантов ответов (`а)`, `б)`, `в)`, `г)`)
- Правильных ответов (маркеры: `[правильн]`, `**`, `✓`)

### 📊 Тестирование и отчеты
- Прохождение тестов с сохранением результатов
- Отслеживание прогресса пользователей
- Генерация отчетов в формате JSON
- Подсчет баллов

## 🗂️ Структура проекта

```
MCHSProject/
├── ConnectionDB/           # Подключение к PostgreSQL
│   └── DBConnect.cs
├── Models/                 # Модели данных
│   ├── User.cs
│   ├── Role.cs
│   ├── Lecture.cs
│   ├── Test.cs
│   ├── Question.cs
│   ├── Answer.cs
│   ├── TestResult.cs
│   ├── UserAnswer.cs
│   ├── Report.cs
│   └── MediaPath.cs
├── DTO/                    # Data Transfer Objects
│   ├── Auth/
│   ├── Users/
│   ├── Lectures/
│   ├── Tests/
│   └── Documents/
├── Services/               # Бизнес-логика
│   ├── Auth/
│   ├── Users/
│   ├── Lectures/
│   ├── Tests/
│   ├── Questions/
│   ├── Documents/
│   └── ...
├── Controllers/            # REST API endpoints
│   ├── AuthController.cs
│   ├── Users/
│   ├── Lectures/
│   ├── Tests/
│   └── Documents/
├── Validators/             # Валидация данных
│   ├── Auth/
│   └── Users/
├── Common/                 # Общие компоненты
│   ├── Exceptions/
│   └── Middleware/
└── Program.cs             # Точка входа
```

## 🛠️ Установка и запуск

### Требования
- .NET SDK 10.0+
- PostgreSQL 16+
- Visual Studio 2022 или VS Code

### 1. Клонирование репозитория
```bash
git clone https://github.com/yourusername/MCHSProject.git
cd MCHSProject
```

### 2. Настройка базы данных
Выполните SQL скрипт для создания таблиц:

```sql
-- Создание базы данных
CREATE DATABASE mchs_db;

-- Подключитесь к БД и выполните миграции из database.sql
```

### 3. Настройка appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=mchs_db;Username=postgres;Password=yourpassword"
  },
  "Jwt": {
    "Secret": "your-super-secret-key-minimum-32-characters-long",
    "Issuer": "MCHSProject",
    "Audience": "MCHSMobileApp",
    "AccessTokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 30
  }
}
```

### 4. Установка зависимостей
```bash
dotnet restore
```

### 5. Запуск приложения
```bash
dotnet run
```

API будет доступен по адресу: `https://localhost:7265`

## 📖 API Документация

После запуска приложения откройте Swagger UI:
```
https://localhost:7265/swagger
```

### Основные endpoints

#### Аутентификация
- `POST /api/Auth/register` - Регистрация
- `POST /api/Auth/login` - Вход
- `POST /api/Auth/refresh-token` - Обновление токена
- `POST /api/Auth/logout` - Выход
- `POST /api/Auth/change-password` - Смена пароля

#### Лекции
- `GET /api/Lectures` - Список всех лекций
- `GET /api/Lectures/{id}` - Получить лекцию
- `POST /api/Lectures` - Создать лекцию
- `PUT /api/Lectures/{id}` - Обновить лекцию
- `DELETE /api/Lectures/{id}` - Удалить лекцию

#### Тесты
- `GET /api/Tests` - Список тестов
- `GET /api/Tests/{id}` - Получить тест
- `POST /api/Tests` - Создать тест
- `PUT /api/Tests/{id}` - Обновить тест
- `DELETE /api/Tests/{id}` - Удалить тест

#### Документы
- `POST /api/Documents/create-test` - Создать тест из PDF/DOCX

### Пример запроса

**Регистрация пользователя:**
```bash
curl -X POST "https://localhost:7265/api/Auth/register" \
  -H "Content-Type: application/json" \
  -d '{
    "username": "admin",
    "password": "Admin123!",
    "roleId": 1
  }'
```

**Создание теста из документа:**
```bash
curl -X POST "https://localhost:7265/api/Documents/create-test" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -F "lectureId=1" \
  -F "testTitle=Пожарная безопасность" \
  -F "createdBy=1" \
  -F "documentFile=@test.pdf"
```

## 🔒 Безопасность

- **Пароли**: Хешируются с помощью BCrypt (Work Factor: 12)
- **JWT**: Access токены живут 60 минут, Refresh - 30 дней
- **SQL Injection**: Защита через параметризованные запросы Dapper
- **CORS**: Настроен для мобильных приложений
- **Валидация**: Проверка всех входящих данных

## 📊 База данных

### Схема таблиц:
- `users` - Пользователи системы
- `roles` - Роли (Админ, Преподаватель, Студент)
- `lectures` - Учебные лекции
- `paths` - Медиа файлы (видео, документы)
- `tests` - Тесты по лекциям
- `questions` - Вопросы в тестах
- `answers` - Варианты ответов
- `test_results` - Результаты прохождения тестов
- `user_answers` - Ответы пользователей
- `reports` - Отчеты

## 🧪 Тестирование

### Формат документов для автоматического парсинга:

```
Вопрос 1: Что делать при пожаре?
а) Вызвать 101 [правильн]
б) Паниковать
в) Убежать
г) Игнорировать

Вопрос 2: Какой огнетушитель для электроприборов?
а) Водный
б) Пенный
в) Углекислотный [правильн]
г) Порошковый
```

## 🚧 Roadmap

- [ ] Уведомления (Push notifications)
- [ ] Экспорт результатов в Excel
- [ ] Интеграция с внешними системами
- [ ] Мобильное приложение (React Native)
- [ ] Кэширование (Redis)
- [ ] Логирование (Serilog)

## 👨‍💻 Автор

Проект разработан для дипломной работы / портфолио

## 📝 Лицензия

MIT License - свободное использование
