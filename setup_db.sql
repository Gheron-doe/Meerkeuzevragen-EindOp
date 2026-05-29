USE master;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'MeerkeuzevragenDB')
BEGIN
    CREATE DATABASE MeerkeuzevragenDB;
END
GO

USE MeerkeuzevragenDB;
GO

DROP TABLE IF EXISTS AttemptAnswer, TestAttempt, TestQuestion, QuestionTopic, Answer, Question, Topic, Test, AppUser;

CREATE TABLE Topic (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1
);

CREATE TABLE Question (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    QuestionText NVARCHAR(1000) NOT NULL,
    DifficultyLevel INT NOT NULL DEFAULT 1,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    Feedback NVARCHAR(200) NULL,
    CONSTRAINT CHK_Question_Difficulty CHECK (DifficultyLevel BETWEEN 1 AND 3)
);

CREATE TABLE Answer (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    QuestionId INT NOT NULL,
    AnswerText NVARCHAR(500) NOT NULL,
    IsCorrect BIT NOT NULL DEFAULT 0,
    Feedback NVARCHAR(200) NULL,
    CONSTRAINT FK_Answer_Question FOREIGN KEY (QuestionId) REFERENCES Question(Id)
);
CREATE INDEX IX_Answer_QuestionId ON Answer (QuestionId);

CREATE TABLE Test (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Title NVARCHAR(200) NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    IsActive BIT NOT NULL DEFAULT 1
);

CREATE TABLE QuestionTopic (
    QuestionId INT NOT NULL,
    TopicId INT NOT NULL,
    PRIMARY KEY (QuestionId, TopicId),
    CONSTRAINT FK_QuestionTopic_Question FOREIGN KEY (QuestionId) REFERENCES Question(Id),
    CONSTRAINT FK_QuestionTopic_Topic FOREIGN KEY (TopicId) REFERENCES Topic(Id)
);
CREATE INDEX IX_QuestionTopic_TopicId ON QuestionTopic (TopicId);

CREATE TABLE TestQuestion (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    TestId INT NOT NULL,
    QuestionId INT NOT NULL,
    SortOrder INT NOT NULL,
    CONSTRAINT FK_TestQuestion_Test FOREIGN KEY (TestId) REFERENCES Test(Id),
    CONSTRAINT FK_TestQuestion_Question FOREIGN KEY (QuestionId) REFERENCES Question(Id)
);
CREATE INDEX IX_TestQuestion_TestId ON TestQuestion (TestId);
CREATE INDEX IX_TestQuestion_QuestionId ON TestQuestion (QuestionId);

CREATE TABLE AppUser (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Username NVARCHAR(100) NOT NULL UNIQUE
);

CREATE TABLE TestAttempt (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    TestId INT NOT NULL,
    UserId INT NOT NULL,
    StartedAt DATETIME NOT NULL DEFAULT GETDATE(),
    CompletedAt DATETIME NULL,
    Feedback NVARCHAR(MAX) NULL,
    CONSTRAINT FK_TestAttempt_Test FOREIGN KEY (TestId) REFERENCES Test(Id),
    CONSTRAINT FK_TestAttempt_AppUser FOREIGN KEY (UserId) REFERENCES AppUser(Id)
);
CREATE INDEX IX_TestAttempt_TestId ON TestAttempt (TestId);
CREATE INDEX IX_TestAttempt_UserId ON TestAttempt (UserId);

CREATE TABLE AttemptAnswer (
    AttemptId INT NOT NULL,
    TestQuestionId INT NOT NULL,
    SelectedAnswerId INT NULL,
    PRIMARY KEY (AttemptId, TestQuestionId),
    CONSTRAINT FK_AttemptAnswer_Attempt FOREIGN KEY (AttemptId) REFERENCES TestAttempt(Id),
    CONSTRAINT FK_AttemptAnswer_TestQuestion FOREIGN KEY (TestQuestionId) REFERENCES TestQuestion(Id),
    CONSTRAINT FK_AttemptAnswer_Answer FOREIGN KEY (SelectedAnswerId) REFERENCES Answer(Id)
);
CREATE INDEX IX_AttemptAnswer_TestQuestionId ON AttemptAnswer (TestQuestionId);
CREATE INDEX IX_AttemptAnswer_SelectedAnswerId ON AttemptAnswer (SelectedAnswerId);
GO