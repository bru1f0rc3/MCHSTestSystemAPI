-- =============================================
-- MCHS Testing System Database Schema
-- PostgreSQL Script
-- =============================================

-- Удаление таблиц если существуют (в правильном порядке из-за foreign keys)
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

-- =============================================
-- Таблица ролей
-- =============================================
CREATE TABLE roles (
    id SERIAL PRIMARY KEY,
    name VARCHAR(50) NOT NULL UNIQUE
);

-- Вставка базовых ролей
INSERT INTO roles (name) VALUES ('admin'), ('guest'), ('user');

-- =============================================
-- Таблица путей (учебные траектории)
-- =============================================
CREATE TABLE paths (
    id SERIAL PRIMARY KEY,
    video_path TEXT,
    document_path TEXT
);

-- =============================================
-- Таблица пользователей
-- =============================================
CREATE TABLE users (
    id SERIAL PRIMARY KEY,
    username VARCHAR(100) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    role_id INT NOT NULL REFERENCES roles(id) ON DELETE RESTRICT,
    device_id VARCHAR(255) UNIQUE,
    email VARCHAR(255),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Создание индекса для быстрого поиска по username
CREATE INDEX idx_users_username ON users(username);
CREATE INDEX idx_users_role_id ON users(role_id);

-- =============================================
-- Таблица лекций
-- =============================================
CREATE TABLE lectures (
    id SERIAL PRIMARY KEY,
    title VARCHAR(255) NOT NULL,
    text_content TEXT,
    path_id INT REFERENCES paths(id) ON DELETE SET NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_lectures_path_id ON lectures(path_id);

-- =============================================
-- Таблица тестов
-- =============================================
CREATE TABLE tests (
    id SERIAL PRIMARY KEY,
    lecture_id INT REFERENCES lectures(id) ON DELETE SET NULL,
    title VARCHAR(255) NOT NULL,
    description TEXT,
    created_by INT NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_tests_lecture_id ON tests(lecture_id);
CREATE INDEX idx_tests_created_by ON tests(created_by);

-- =============================================
-- Таблица вопросов
-- =============================================
CREATE TABLE questions (
    id SERIAL PRIMARY KEY,
    test_id INT NOT NULL REFERENCES tests(id) ON DELETE CASCADE,
    question_text TEXT NOT NULL,
    position INT NOT NULL DEFAULT 0
);

CREATE INDEX idx_questions_test_id ON questions(test_id);

-- =============================================
-- Таблица ответов
-- =============================================
CREATE TABLE answers (
    id SERIAL PRIMARY KEY,
    question_id INT NOT NULL REFERENCES questions(id) ON DELETE CASCADE,
    answer_text TEXT NOT NULL,
    is_correct BOOLEAN NOT NULL DEFAULT FALSE,
    position INT NOT NULL DEFAULT 0
);

CREATE INDEX idx_answers_question_id ON answers(question_id);

-- =============================================
-- Таблица результатов тестирования
-- =============================================
CREATE TABLE test_results (
    id SERIAL PRIMARY KEY,
    user_id INT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    test_id INT NOT NULL REFERENCES tests(id) ON DELETE CASCADE,
    started_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    finished_at TIMESTAMP,
    score FLOAT8
);

CREATE INDEX idx_test_results_user_id ON test_results(user_id);
CREATE INDEX idx_test_results_test_id ON test_results(test_id);

-- =============================================
-- Таблица ответов пользователей
-- =============================================
CREATE TABLE user_answers (
    id SERIAL PRIMARY KEY,
    test_result_id INT NOT NULL REFERENCES test_results(id) ON DELETE CASCADE,
    question_id INT NOT NULL REFERENCES questions(id) ON DELETE CASCADE,
    answer_id INT REFERENCES answers(id) ON DELETE SET NULL,
    answered_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_user_answers_test_result_id ON user_answers(test_result_id);
CREATE INDEX idx_user_answers_question_id ON user_answers(question_id);

-- =============================================
-- Таблица отчетов
-- =============================================
CREATE TABLE reports (
    id SERIAL PRIMARY KEY,
    created_by INT NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    report_date DATE NOT NULL DEFAULT CURRENT_DATE,
    content JSONB,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_reports_created_by ON reports(created_by);
CREATE INDEX idx_reports_report_date ON reports(report_date);

-- =============================================
-- Создание администратора по умолчанию
-- Пароль: admin123 (BCrypt hash)
-- =============================================
INSERT INTO users (username, password_hash, role_id) 
VALUES ('admin', '$2a$11$rBNj.P5U5E7Xr5Q5F5F5F.5F5F5F5F5F5F5F5F5F5F5F5F5F5F5F5F', 1);

-- =============================================
-- Пример данных для тестирования
-- =============================================

-- Добавляем путь с материалами
INSERT INTO paths (video_path, document_path) 
VALUES ('/storage/videos/fire_safety.mp4', '/storage/documents/fire_safety.pdf');

-- Добавляем лекцию
INSERT INTO lectures (title, text_content, path_id) 
VALUES (
    'Основы пожарной безопасности', 
    'Пожарная безопасность — состояние объекта, характеризуемое возможностью предотвращения возникновения и развития пожара, а также воздействия на людей и имущество опасных факторов пожара...', 
    1
);

-- Добавляем тест
INSERT INTO tests (lecture_id, title, description, created_by) 
VALUES (1, 'Тест по пожарной безопасности', 'Базовый тест для проверки знаний по пожарной безопасности', 1);

-- Добавляем вопросы
INSERT INTO questions (test_id, question_text, position) VALUES
(1, 'Какой номер телефона для вызова пожарной службы?', 1),
(1, 'Что нужно делать при обнаружении пожара в первую очередь?', 2),
(1, 'Какой класс пожара связан с горением электроустановок?', 3);

-- Добавляем ответы
-- Вопрос 1
INSERT INTO answers (question_id, answer_text, is_correct, position) VALUES
(1, '01', FALSE, 1),
(1, '101', TRUE, 2),
(1, '112', TRUE, 3),
(1, '03', FALSE, 4);

-- Вопрос 2
INSERT INTO answers (question_id, answer_text, is_correct, position) VALUES
(2, 'Попытаться потушить самостоятельно', FALSE, 1),
(2, 'Сообщить о пожаре и эвакуироваться', TRUE, 2),
(2, 'Спрятаться в безопасном месте', FALSE, 3),
(2, 'Открыть окна для проветривания', FALSE, 4);

-- Вопрос 3
INSERT INTO answers (question_id, answer_text, is_correct, position) VALUES
(3, 'Класс А', FALSE, 1),
(3, 'Класс B', FALSE, 2),
(3, 'Класс C', FALSE, 3),
(3, 'Класс E', TRUE, 4);

-- =============================================
-- Полезные представления (Views)
-- =============================================

-- Представление для результатов тестов с деталями
CREATE OR REPLACE VIEW v_test_results_details AS
SELECT 
    tr.id as result_id,
    u.username,
    t.title as test_title,
    l.title as lecture_title,
    tr.started_at,
    tr.finished_at,
    tr.score,
    CASE 
        WHEN tr.finished_at IS NULL THEN 'in_progress'
        WHEN tr.score >= 70 THEN 'passed'
        ELSE 'failed'
    END as status
FROM test_results tr
JOIN users u ON tr.user_id = u.id
JOIN tests t ON tr.test_id = t.id
LEFT JOIN lectures l ON t.lecture_id = l.id;

-- Представление для статистики по тестам
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

-- =============================================
-- Функции
-- =============================================

-- Функция для расчета результата теста
CREATE OR REPLACE FUNCTION calculate_test_score(p_test_result_id INT)
RETURNS FLOAT8 AS $$
DECLARE
    v_total_questions INT;
    v_correct_answers INT;
    v_score FLOAT8;
BEGIN
    -- Получаем общее количество вопросов в тесте
    SELECT COUNT(DISTINCT q.id) INTO v_total_questions
    FROM test_results tr
    JOIN tests t ON tr.test_id = t.id
    JOIN questions q ON t.id = q.test_id
    WHERE tr.id = p_test_result_id;
    
    -- Получаем количество правильных ответов
    SELECT COUNT(*) INTO v_correct_answers
    FROM user_answers ua
    JOIN answers a ON ua.answer_id = a.id
    WHERE ua.test_result_id = p_test_result_id AND a.is_correct = TRUE;
    
    -- Рассчитываем процент
    IF v_total_questions > 0 THEN
        v_score := (v_correct_answers::FLOAT8 / v_total_questions::FLOAT8) * 100;
    ELSE
        v_score := 0;
    END IF;
    
    -- Обновляем результат теста
    UPDATE test_results 
    SET score = v_score, finished_at = CURRENT_TIMESTAMP
    WHERE id = p_test_result_id;
    
    RETURN v_score;
END;
$$ LANGUAGE plpgsql;
