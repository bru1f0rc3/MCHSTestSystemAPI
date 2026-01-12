# MCHSWebAPI Test Project

Проект содержит unit-тесты для MCHSWebAPI.

## Структура тестов

- **Controllers/** - тесты контроллеров
  - AuthControllerTests
  - UsersControllerTests
  - LecturesControllerTests
  - TestsControllerTests
  - TestingControllerTests
  - ReportsControllerTests
  - RolesControllerTests

- **Services/** - тесты сервисов
  - AuthServiceTests
  - UserServiceTests
  - LectureServiceTests
  - TestServiceTests
  - TestingServiceTests
  - ReportServiceTests

- **Repositories/** - тесты репозиториев
  - UserRepositoryTests
  - LectureRepositoryTests
  - TestRepositoryTests
  - QuestionRepositoryTests
  - AnswerRepositoryTests
  - TestResultRepositoryTests
  - ReportRepositoryTests

- **Helpers/** - вспомогательные классы
  - TestDataFactory - фабрика тестовых данных

## Технологии

- **xUnit** - фреймворк для тестирования
- **Moq** - библиотека для создания mock-объектов
- **FluentAssertions** - библиотека для удобных assertions

## Запуск тестов

```bash
dotnet test
```

## Покрытие кода

Для просмотра покрытия кода тестами:

```bash
dotnet test /p:CollectCoverage=true
```
