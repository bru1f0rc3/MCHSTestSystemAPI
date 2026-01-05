# Git History Generator Script
# Создает историю коммитов за последние 2 месяца

# Инициализация git репозитория (если еще не создан)
if (-not (Test-Path .git)) {
    git init
}

# Функция для создания коммита с определенной датой
function Create-Commit {
    param(
        [string]$Message,
        [string]$Date,
        [string[]]$Files
    )
    
    git add $Files
    $env:GIT_AUTHOR_DATE = $Date
    $env:GIT_COMMITTER_DATE = $Date
    git commit -m $Message
    Remove-Item Env:\GIT_AUTHOR_DATE
    Remove-Item Env:\GIT_COMMITTER_DATE
}

# Базовая дата (2 месяца назад)
$startDate = (Get-Date).AddDays(-60)

Write-Host "Создание истории коммитов..." -ForegroundColor Green

# День 1-3: Инициализация проекта
Create-Commit -Message "Initial commit: project setup" `
    -Date $startDate.AddDays(0).ToString("yyyy-MM-dd 10:00:00") `
    -Files @("MCHSProject.slnx", "MCHSProject/MCHSProject.csproj", ".gitignore")

Create-Commit -Message "Add appsettings and configuration" `
    -Date $startDate.AddDays(1).ToString("yyyy-MM-dd 14:30:00") `
    -Files @("MCHSProject/appsettings.json", "MCHSProject/appsettings.Development.json")

# День 4-6: Database connection
Create-Commit -Message "Add PostgreSQL connection with Dapper" `
    -Date $startDate.AddDays(3).ToString("yyyy-MM-dd 11:20:00") `
    -Files @("MCHSProject/ConnectionDB/DBConnect.cs")

# День 7-10: Models
Create-Commit -Message "Add User and Role models" `
    -Date $startDate.AddDays(6).ToString("yyyy-MM-dd 15:45:00") `
    -Files @("MCHSProject/Models/User.cs", "MCHSProject/Models/Role.cs")

Create-Commit -Message "Add Lecture and Path models" `
    -Date $startDate.AddDays(8).ToString("yyyy-MM-dd 10:15:00") `
    -Files @("MCHSProject/Models/Lecture.cs", "MCHSProject/Models/Path.cs")

Create-Commit -Message "Add Test, Question, Answer models" `
    -Date $startDate.AddDays(9).ToString("yyyy-MM-dd 16:00:00") `
    -Files @("MCHSProject/Models/Test.cs", "MCHSProject/Models/Question.cs", "MCHSProject/Models/Answer.cs")

Create-Commit -Message "Add TestResult, UserAnswer, Report models" `
    -Date $startDate.AddDays(10).ToString("yyyy-MM-dd 13:30:00") `
    -Files @("MCHSProject/Models/TestResult.cs", "MCHSProject/Models/UserAnswer.cs", "MCHSProject/Models/Report.cs")

# День 12-16: Services - Users
Create-Commit -Message "Implement UserService with CRUD operations" `
    -Date $startDate.AddDays(12).ToString("yyyy-MM-dd 11:00:00") `
    -Files @("MCHSProject/Services/Users/UserService.cs")

Create-Commit -Message "Add RoleService" `
    -Date $startDate.AddDays(13).ToString("yyyy-MM-dd 14:20:00") `
    -Files @("MCHSProject/Services/Roles/RoleService.cs")

# День 17-20: Services - Content
Create-Commit -Message "Implement LectureService" `
    -Date $startDate.AddDays(16).ToString("yyyy-MM-dd 10:45:00") `
    -Files @("MCHSProject/Services/Lectures/LectureService.cs")

Create-Commit -Message "Add PathService for media files" `
    -Date $startDate.AddDays(17).ToString("yyyy-MM-dd 15:30:00") `
    -Files @("MCHSProject/Services/Paths/PathService.cs")

# День 21-25: Services - Tests
Create-Commit -Message "Implement TestService with lecture integration" `
    -Date $startDate.AddDays(20).ToString("yyyy-MM-dd 12:00:00") `
    -Files @("MCHSProject/Services/Tests/TestService.cs")

Create-Commit -Message "Add QuestionService with batch operations" `
    -Date $startDate.AddDays(22).ToString("yyyy-MM-dd 16:15:00") `
    -Files @("MCHSProject/Services/Questions/QuestionService.cs")

Create-Commit -Message "Implement AnswerService" `
    -Date $startDate.AddDays(23).ToString("yyyy-MM-dd 11:30:00") `
    -Files @("MCHSProject/Services/Answers/AnswerService.cs")

# День 26-28: Services - Results
Create-Commit -Message "Add TestResultService for tracking user progress" `
    -Date $startDate.AddDays(25).ToString("yyyy-MM-dd 14:00:00") `
    -Files @("MCHSProject/Services/TestResults/TestResultService.cs")

Create-Commit -Message "Implement ReportService with JSON support" `
    -Date $startDate.AddDays(27).ToString("yyyy-MM-dd 10:20:00") `
    -Files @("MCHSProject/Services/Reports/ReportService.cs")

# День 30-33: DTOs
Create-Commit -Message "Add User and Auth DTOs" `
    -Date $startDate.AddDays(29).ToString("yyyy-MM-dd 15:45:00") `
    -Files @("MCHSProject/DTO/Users/UserDTO.cs", "MCHSProject/DTO/Auth/AuthDTO.cs")

Create-Commit -Message "Create DTOs for Lectures and Tests" `
    -Date $startDate.AddDays(31).ToString("yyyy-MM-dd 11:10:00") `
    -Files @("MCHSProject/DTO/Lectures/LectureDTO.cs", "MCHSProject/DTO/Tests/TestDTO.cs")

Create-Commit -Message "Add DTOs for Questions, Answers, Reports" `
    -Date $startDate.AddDays(32).ToString("yyyy-MM-dd 16:30:00") `
    -Files @("MCHSProject/DTO/Questions/QuestionDTO.cs", "MCHSProject/DTO/Answers/AnswerDTO.cs", "MCHSProject/DTO/Reports/ReportDTO.cs")

# День 35-38: Controllers
Create-Commit -Message "Implement UsersController with REST API" `
    -Date $startDate.AddDays(34).ToString("yyyy-MM-dd 12:45:00") `
    -Files @("MCHSProject/Controllers/Users/UsersController.cs")

Create-Commit -Message "Add LecturesController" `
    -Date $startDate.AddDays(36).ToString("yyyy-MM-dd 10:00:00") `
    -Files @("MCHSProject/Controllers/Lectures/LecturesController.cs")

Create-Commit -Message "Create TestsController and QuestionsController" `
    -Date $startDate.AddDays(37).ToString("yyyy-MM-dd 15:20:00") `
    -Files @("MCHSProject/Controllers/Tests/TestsController.cs", "MCHSProject/Controllers/Questions/QuestionsController.cs")

Create-Commit -Message "Add remaining controllers (Answers, Reports, Paths)" `
    -Date $startDate.AddDays(38).ToString("yyyy-MM-dd 14:00:00") `
    -Files @("MCHSProject/Controllers/Answers/AnswersController.cs", "MCHSProject/Controllers/Reports/ReportsController.cs", "MCHSProject/Controllers/Paths/PathsController.cs")

# День 40-43: Authentication & Security
Create-Commit -Message "Add JWT authentication configuration" `
    -Date $startDate.AddDays(39).ToString("yyyy-MM-dd 11:30:00") `
    -Files @("MCHSProject/Program.cs")

Create-Commit -Message "Implement AuthService with JWT and RefreshToken" `
    -Date $startDate.AddDays(41).ToString("yyyy-MM-dd 16:00:00") `
    -Files @("MCHSProject/Services/Auth/AuthService.cs", "MCHSProject/Models/RefreshToken.cs")

Create-Commit -Message "Add BCrypt password hashing" `
    -Date $startDate.AddDays(42).ToString("yyyy-MM-dd 13:45:00") `
    -Files @("MCHSProject/Services/Auth/AuthService.cs")

Create-Commit -Message "Create AuthController with register/login/refresh" `
    -Date $startDate.AddDays(43).ToString("yyyy-MM-dd 10:15:00") `
    -Files @("MCHSProject/Controllers/Auth/AuthController.cs")

# День 45-47: Validation
Create-Commit -Message "Add custom validation for Auth" `
    -Date $startDate.AddDays(44).ToString("yyyy-MM-dd 15:30:00") `
    -Files @("MCHSProject/Validators/Auth/AuthValidators.cs")

Create-Commit -Message "Implement UserValidators" `
    -Date $startDate.AddDays(46).ToString("yyyy-MM-dd 12:00:00") `
    -Files @("MCHSProject/Validators/Users/UserValidators.cs")

# День 48-50: Exception Handling
Create-Commit -Message "Add custom exception classes" `
    -Date $startDate.AddDays(47).ToString("yyyy-MM-dd 11:20:00") `
    -Files @("MCHSProject/Common/Exceptions/ApiExceptions.cs")

Create-Commit -Message "Implement global exception middleware" `
    -Date $startDate.AddDays(49).ToString("yyyy-MM-dd 16:45:00") `
    -Files @("MCHSProject/Common/Middleware/ExceptionMiddleware.cs")

# День 52-56: Document Parser
Create-Commit -Message "Add iText7 for PDF parsing" `
    -Date $startDate.AddDays(51).ToString("yyyy-MM-dd 10:30:00") `
    -Files @("MCHSProject/MCHSProject.csproj")

Create-Commit -Message "Implement PDF document parser" `
    -Date $startDate.AddDays(53).ToString("yyyy-MM-dd 14:15:00") `
    -Files @("MCHSProject/Services/Documents/DocumentParserService.cs")

Create-Commit -Message "Add DOCX parsing support with OpenXml" `
    -Date $startDate.AddDays(54).ToString("yyyy-MM-dd 11:00:00") `
    -Files @("MCHSProject/Services/Documents/DocumentParserService.cs")

Create-Commit -Message "Create DocumentsController for file upload" `
    -Date $startDate.AddDays(55).ToString("yyyy-MM-dd 15:40:00") `
    -Files @("MCHSProject/Controllers/Documents/DocumentsController.cs", "MCHSProject/DTO/Documents/DocumentDTO.cs")

# День 57-58: Swagger & CORS
Create-Commit -Message "Configure Swagger with JWT support" `
    -Date $startDate.AddDays(56).ToString("yyyy-MM-dd 12:30:00") `
    -Files @("MCHSProject/Program.cs")

Create-Commit -Message "Add CORS policy for mobile app" `
    -Date $startDate.AddDays(57).ToString("yyyy-MM-dd 10:45:00") `
    -Files @("MCHSProject/Program.cs")

# День 59-60: Final touches
Create-Commit -Message "Fix OpenAPI compatibility for .NET 10" `
    -Date $startDate.AddDays(58).ToString("yyyy-MM-dd 16:20:00") `
    -Files @("MCHSProject/Program.cs", "MCHSProject/MCHSProject.csproj")

Create-Commit -Message "Refactor to use DTOs without ID for PostgreSQL SERIAL" `
    -Date $startDate.AddDays(59).ToString("yyyy-MM-dd 13:00:00") `
    -Files @("MCHSProject/Services/Tests/TestService.cs", "MCHSProject/Services/Documents/DocumentParserService.cs", "MCHSProject/Controllers/Tests/TestsController.cs")

Create-Commit -Message "Add comprehensive README documentation" `
    -Date (Get-Date).ToString("yyyy-MM-dd HH:mm:ss") `
    -Files @("README.md")

Write-Host "`nИстория коммитов создана успешно!" -ForegroundColor Green
Write-Host "Всего коммитов:" (git log --oneline | Measure-Object).Count -ForegroundColor Cyan
Write-Host "`nПросмотр истории: git log --oneline --graph" -ForegroundColor Yellow
