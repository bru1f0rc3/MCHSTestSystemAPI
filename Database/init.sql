-- =============================================
-- MCHS Testing System Database Schema
-- PostgreSQL Script
-- =============================================

-- Удаление таблиц если существуют (в правильном порядке из-за foreign keys)
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
DROP TABLE IF EXISTS verification_codes CASCADE;

DROP VIEW IF EXISTS v_test_results_details CASCADE;
DROP VIEW IF EXISTS v_test_statistics CASCADE;
DROP FUNCTION IF EXISTS calculate_test_score(INT) CASCADE;

-- =============================================
-- Таблица ролей
-- =============================================
CREATE TABLE roles (
    id SERIAL PRIMARY KEY,
    name VARCHAR(50) NOT NULL UNIQUE
);

INSERT INTO roles (name) VALUES ('admin'), ('guest'), ('user');

-- =============================================
-- Таблица путей (учебные материалы)
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
    email_verified BOOLEAN NOT NULL DEFAULT FALSE,
    pending_email VARCHAR(255),
    pending_email_verified BOOLEAN NOT NULL DEFAULT FALSE,
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

-- =============================================
-- Таблица кодов подтверждения
-- =============================================
CREATE TABLE verification_codes (
    id SERIAL PRIMARY KEY,
    email VARCHAR(255) NOT NULL,
    code VARCHAR(6) NOT NULL,
    purpose VARCHAR(50) NOT NULL,
    expires_at TIMESTAMP NOT NULL,
    used BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_verification_codes_email ON verification_codes(email, purpose);

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
--   time_limit_minutes — лимит по времени на прохождение теста
--   passing_score      — проходной балл (%)
-- =============================================
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
--   cheat_attempts  — кол-во зафиксированных попыток списывания
--                    (сворачивание приложения, потеря фокуса и т.п.)
--   auto_submitted  — был ли тест завершён автоматически (по таймауту)
-- =============================================
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
-- Таблица событий возможного списывания (журнал)
-- =============================================
CREATE TABLE cheat_events (
    id SERIAL PRIMARY KEY,
    test_result_id INT NOT NULL REFERENCES test_results(id) ON DELETE CASCADE,
    event_type VARCHAR(50) NOT NULL,
    details TEXT,
    occurred_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_cheat_events_test_result_id ON cheat_events(test_result_id);

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
-- Администратор по умолчанию создаётся автоматически при старте backend
-- (см. MCHSWebAPI/Data/DatabaseInitializer.cs)
-- Login: admin / Password: admin123
-- =============================================

-- =============================================
-- Представления (Views)
-- =============================================

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

-- =============================================
-- Функции
-- =============================================

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

