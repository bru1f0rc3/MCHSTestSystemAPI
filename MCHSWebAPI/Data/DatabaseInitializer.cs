using Dapper;

namespace MCHSWebAPI.Data;

/// <summary>
/// Подготавливает базу данных при запуске приложения:
/// обновляет схему, создаёт стандартных админов и наполняет демо-данными
/// </summary>
public static class DatabaseInitializer
{
    private const string DefaultAdminUsername = "admin";
    private const string DefaultAdminPassword = "admin123";
    private const string DefaultSuperAdminUsername = "superadmin";
    private const string DefaultSuperAdminPassword = "superadmin123";

    /// <summary>
    /// Обновляет схему базы: добавляет недостающие столбцы (ФИО), индекс по почте
    /// и роль суперадмина. Все ошибки только записываются в лог
    /// </summary>
    /// <param name="factory">Фабрика для создания подключения к базе</param>
    /// <param name="logger">Журнал для записи ошибок (можно не указывать)</param>
    public static async Task MigrateSchemaAsync(IDbConnectionFactory factory, ILogger? logger = null)
    {
        try
        {
            using var connection = factory.CreateConnection();
            await connection.ExecuteAsync(
                @"ALTER TABLE users ADD COLUMN IF NOT EXISTS last_name  VARCHAR(100);
                  ALTER TABLE users ADD COLUMN IF NOT EXISTS first_name VARCHAR(100);
                  ALTER TABLE users ADD COLUMN IF NOT EXISTS patronymic VARCHAR(100);
                  CREATE UNIQUE INDEX IF NOT EXISTS uq_users_email_not_null
                    ON users (email)
                    WHERE email IS NOT NULL;
                  INSERT INTO roles (name) VALUES ('superadmin')
                    ON CONFLICT (name) DO NOTHING;");
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Ошибка миграции схемы.");
        }
    }

    /// <summary>
    /// Создаёт стандартные аккаунты администратора и суперадминистратора,
    /// если их ещё нет в базе
    /// </summary>
    /// <param name="factory">Фабрика для создания подключения к базе</param>
    /// <param name="logger">Журнал для записи ошибок (можно не указывать)</param>
    public static async Task EnsureDefaultAdminAsync(IDbConnectionFactory factory, ILogger? logger = null)
    {
        try
        {
            using var connection = factory.CreateConnection();

            await EnsureDefaultUserAsync(
                connection, "superadmin",
                DefaultSuperAdminUsername, DefaultSuperAdminPassword, logger);

            await EnsureDefaultUserAsync(
                connection, "admin",
                DefaultAdminUsername, DefaultAdminPassword, logger);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Ошибка при создании дефолтных аккаунтов.");
        }
    }

    /// <summary>
    /// Создаёт одного стандартного пользователя с заданной ролью, если его ещё нет.
    /// Пароль сохраняется в зашифрованном виде
    /// </summary>
    /// <param name="connection">Открытое подключение к базе данных</param>
    /// <param name="roleName">Название роли пользователя (например, "admin")</param>
    /// <param name="username">Логин создаваемого пользователя</param>
    /// <param name="password">Пароль создаваемого пользователя</param>
    /// <param name="logger">Журнал для записи сообщений (можно не указывать)</param>
    private static async Task EnsureDefaultUserAsync(
        System.Data.IDbConnection connection,
        string roleName, string username, string password, ILogger? logger)
    {
        var roleId = await connection.ExecuteScalarAsync<int?>(
            "SELECT id FROM roles WHERE name = @Name LIMIT 1",
            new { Name = roleName });

        if (roleId == null)
        {
            logger?.LogWarning("Роль '{Role}' не найдена — пропускаем создание дефолтного пользователя {User}.",
                roleName, username);
            return;
        }

        var exists = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM users WHERE username = @Username",
            new { Username = username });

        if (exists > 0) return;

        var hash = BCrypt.Net.BCrypt.HashPassword(password);
        await connection.ExecuteAsync(
            @"INSERT INTO users (username, password_hash, role_id)
              VALUES (@Username, @Hash, @RoleId)",
            new { Username = username, Hash = hash, RoleId = roleId.Value });

        logger?.LogInformation(
            "Создан дефолтный {Role}: {User} / {Pass}. Рекомендуется сменить пароль.",
            roleName, username, password);
    }
    /// <summary>
    /// Наполняет базу демонстрационными данными: 3 лекции, 3 теста и вопросы к ним.
    /// Уже существующие записи не дублируются
    /// </summary>
    /// <param name="factory">Фабрика для создания подключения к базе</param>
    /// <param name="logger">Журнал для записи сообщений (можно не указывать)</param>
    public static async Task SeedSampleDataAsync(IDbConnectionFactory factory, ILogger? logger = null)
    {
        try
        {
            using var connection = factory.CreateConnection();
            connection.Open();

            var adminId = await connection.ExecuteScalarAsync<int?>(
                "SELECT id FROM users WHERE username = 'admin' LIMIT 1");
            if (adminId == null)
            {
                logger?.LogWarning("Не найден админ — пропуск сид-данных.");
                return;
            }

            using var tx = connection.BeginTransaction();
            try
            {
                var lecture1Id = await EnsureLectureAsync(connection, tx,
                    "Основы пожарной безопасности",
                    "Пожарная безопасность — комплекс мер, направленных на предотвращение возгораний и их последствий.\n\n" +
                    "Основные правила:\n" +
                    "1. Не оставлять открытые источники огня без присмотра.\n" +
                    "2. Соблюдать требования при эксплуатации электроприборов.\n" +
                    "3. Знать расположение ближайшего огнетушителя и эвакуационного выхода.\n" +
                    "4. При обнаружении возгорания немедленно сообщить по номеру 101 (МЧС).");

                var lecture2Id = await EnsureLectureAsync(connection, tx,
                    "Оказание первой помощи",
                    "Первая помощь — срочные мероприятия до прибытия медиков.\n\n" +
                    "Основные шаги:\n" +
                    "1. Убедиться в безопасности места происшествия.\n" +
                    "2. Оценить состояние пострадавшего.\n" +
                    "3. Вызвать скорую (103 / 112).\n" +
                    "4. При отсутствии дыхания — начать СЛР (30 компрессий / 2 вдоха).\n" +
                    "5. Остановить наружное кровотечение жгутом или давящей повязкой.");

                var lecture3Id = await EnsureLectureAsync(connection, tx,
                    "Действия при ЧС природного характера",
                    "Наводнения, землетрясения, ураганы — требуют знания алгоритма действий.\n\n" +
                    "При наводнении: эвакуация на возвышенность, отключение электричества и газа.\n" +
                    "При землетрясении: укрытие в дверном проёме/под столом, удаление от окон.\n" +
                    "При урагане: укрытие в капитальном помещении вдали от окон.");

                var test1Id = await InsertTestAsync(connection, tx,
                    lecture1Id, "Пожарная безопасность: базовый тест",
                    "Проверка знаний по базовым правилам пожарной безопасности. 5 вопросов, 10 минут.",
                    10, 70, adminId.Value);

                await InsertQuestionWithAnswersAsync(connection, tx, test1Id, 1,
                    "По какому номеру в России вызывают пожарную охрану?",
                    new[] { ("101", true), ("103", false), ("112 — только скорая", false), ("02", false) });
                await InsertQuestionWithAnswersAsync(connection, tx, test1Id, 2,
                    "Каким огнетушителем НЕЛЬЗЯ тушить электропроводку под напряжением?",
                    new[] { ("Водным (ОВ)", true), ("Углекислотным (ОУ)", false), ("Порошковым (ОП)", false) });
                await InsertQuestionWithAnswersAsync(connection, tx, test1Id, 3,
                    "Что нужно сделать в первую очередь при возгорании в помещении?",
                    new[]
                    {
                        ("Сообщить о пожаре и начать эвакуацию", true),
                        ("Собрать личные вещи", false),
                        ("Открыть все окна для проветривания", false),
                        ("Использовать лифт для эвакуации", false)
                    });
                await InsertQuestionWithAnswersAsync(connection, tx, test1Id, 4,
                    "Какие из перечисленных действий являются первичными мерами пожаротушения? (несколько)",
                    new[]
                    {
                        ("Применение огнетушителя", true),
                        ("Использование пожарного крана", true),
                        ("Накрывание очага плотной тканью", true),
                        ("Заливание горящей электропроводки водой", false)
                    });
                await InsertQuestionWithAnswersAsync(connection, tx, test1Id, 5,
                    "Как безопасно покидать задымлённое помещение?",
                    new[]
                    {
                        ("Пригнувшись, прикрыв дыхательные пути влажной тканью", true),
                        ("В полный рост, глубоко дыша", false),
                        ("Бегом, как можно быстрее, любой дорогой", false)
                    });

                var test2Id = await InsertTestAsync(connection, tx,
                    lecture2Id, "Первая помощь: основные навыки",
                    "Базовые вопросы по оказанию первой помощи пострадавшему. 4 вопроса, 8 минут.",
                    8, 75, adminId.Value);

                await InsertQuestionWithAnswersAsync(connection, tx, test2Id, 1,
                    "Каково соотношение компрессий и вдохов при СЛР у взрослого?",
                    new[] { ("30:2", true), ("15:2", false), ("5:1", false), ("10:2", false) });
                await InsertQuestionWithAnswersAsync(connection, tx, test2Id, 2,
                    "На какое время максимум накладывается кровоостанавливающий жгут летом?",
                    new[]
                    {
                        ("Не более 1 часа", true),
                        ("Не более 30 минут", false),
                        ("Не более 2 часов", false),
                        ("До прибытия медиков, без ограничений", false)
                    });
                await InsertQuestionWithAnswersAsync(connection, tx, test2Id, 3,
                    "Что из перечисленного относится к признакам клинической смерти? (несколько)",
                    new[]
                    {
                        ("Отсутствие сознания", true),
                        ("Отсутствие дыхания", true),
                        ("Отсутствие пульса на сонной артерии", true),
                        ("Наличие устойчивого пульса", false)
                    });
                await InsertQuestionWithAnswersAsync(connection, tx, test2Id, 4,
                    "При подозрении на перелом позвоночника пострадавшего следует:",
                    new[]
                    {
                        ("Не двигать до прибытия врачей, зафиксировать шею", true),
                        ("Перевернуть на бок", false),
                        ("Посадить и дать попить", false)
                    });

                var test3Id = await InsertTestAsync(connection, tx,
                    lecture3Id, "ЧС природного характера",
                    "Короткий тест без ограничения по времени.",
                    null, 60, adminId.Value);

                await InsertQuestionWithAnswersAsync(connection, tx, test3Id, 1,
                    "При землетрясении в многоэтажном здании предпочтительнее:",
                    new[]
                    {
                        ("Укрыться под прочной мебелью или в дверном проёме", true),
                        ("Выбежать по лестнице наружу", false),
                        ("Воспользоваться лифтом", false)
                    });
                await InsertQuestionWithAnswersAsync(connection, tx, test3Id, 2,
                    "Что из перечисленного является первичными признаками наводнения? (несколько)",
                    new[]
                    {
                        ("Резкий подъём уровня воды в реках", true),
                        ("Продолжительные сильные осадки", true),
                        ("Солнечная ясная погода", false)
                    });

                tx.Commit();
                logger?.LogInformation("Демо-данные успешно добавлены: 3 лекции, 3 теста, 11 вопросов.");
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Ошибка при наполнении демо-данными.");
        }
    }

    /// <summary>
    /// Добавляет лекцию, если её ещё нет, и возвращает её номер.
    /// Если лекция с таким названием уже есть — возвращает её номер
    /// </summary>
    /// <param name="conn">Открытое подключение к базе данных</param>
    /// <param name="tx">Транзакция, в рамках которой выполняется вставка</param>
    /// <param name="title">Название лекции</param>
    /// <param name="content">Текст лекции</param>
    private static async Task<int> EnsureLectureAsync(
        System.Data.IDbConnection conn, System.Data.IDbTransaction tx,
        string title, string content)
    {
        var existing = await conn.ExecuteScalarAsync<int?>(
            "SELECT id FROM lectures WHERE title = @Title LIMIT 1",
            new { Title = title }, tx);
        if (existing.HasValue) return existing.Value;

        return await conn.ExecuteScalarAsync<int>(
            @"INSERT INTO lectures (title, text_content) VALUES (@Title, @Text) RETURNING id",
            new { Title = title, Text = content }, tx);
    }

    /// <summary>
    /// Добавляет тест, если его ещё нет, и возвращает его номер.
    /// Если тест с таким названием уже есть — возвращает его номер
    /// </summary>
    /// <param name="conn">Открытое подключение к базе данных</param>
    /// <param name="tx">Транзакция, в рамках которой выполняется вставка</param>
    /// <param name="lectureId">Номер лекции, к которой привязан тест</param>
    /// <param name="title">Название теста</param>
    /// <param name="description">Описание теста</param>
    /// <param name="timeLimit">Ограничение по времени в минутах (можно не указывать)</param>
    /// <param name="passingScore">Проходной балл</param>
    /// <param name="createdBy">Номер пользователя, который создаёт тест</param>
    private static async Task<int> InsertTestAsync(
        System.Data.IDbConnection conn, System.Data.IDbTransaction tx,
        int lectureId, string title, string description, int? timeLimit, int passingScore, int createdBy)
    {
        var existing = await conn.ExecuteScalarAsync<int?>(
            "SELECT id FROM tests WHERE title = @Title LIMIT 1",
            new { Title = title }, tx);
        if (existing.HasValue) return existing.Value;

        return await conn.ExecuteScalarAsync<int>(
            @"INSERT INTO tests (lecture_id, title, description, time_limit_minutes, passing_score, created_by)
              VALUES (@LectureId, @Title, @Description, @TimeLimit, @PassingScore, @CreatedBy)
              RETURNING id",
            new
            {
                LectureId = lectureId,
                Title = title,
                Description = description,
                TimeLimit = timeLimit,
                PassingScore = passingScore,
                CreatedBy = createdBy
            }, tx);
    }

    /// <summary>
    /// Добавляет в тест вопрос вместе с вариантами ответов.
    /// Если такой вопрос в тесте уже есть — ничего не делает
    /// </summary>
    /// <param name="conn">Открытое подключение к базе данных</param>
    /// <param name="tx">Транзакция, в рамках которой выполняется вставка</param>
    /// <param name="testId">Номер теста, в который добавляем вопрос</param>
    /// <param name="position">Порядковый номер вопроса в тесте</param>
    /// <param name="questionText">Текст вопроса</param>
    /// <param name="answers">Список ответов: текст и признак правильности</param>
    private static async Task InsertQuestionWithAnswersAsync(
        System.Data.IDbConnection conn, System.Data.IDbTransaction tx,
        int testId, int position, string questionText, (string Text, bool IsCorrect)[] answers)
    {
        var existingQuestionId = await conn.ExecuteScalarAsync<int?>(
            @"SELECT id FROM questions WHERE test_id = @TestId AND question_text = @Text LIMIT 1",
            new { TestId = testId, Text = questionText }, tx);
        if (existingQuestionId.HasValue) return;

        var questionId = await conn.ExecuteScalarAsync<int>(
            @"INSERT INTO questions (test_id, question_text, position)
              VALUES (@TestId, @Text, @Position) RETURNING id",
            new { TestId = testId, Text = questionText, Position = position }, tx);

        for (var i = 0; i < answers.Length; i++)
        {
            var (text, isCorrect) = answers[i];
            await conn.ExecuteAsync(
                @"INSERT INTO answers (question_id, answer_text, is_correct, position)
                  VALUES (@QuestionId, @Text, @IsCorrect, @Position)",
                new { QuestionId = questionId, Text = text, IsCorrect = isCorrect, Position = i + 1 }, tx);
        }
    }
}
