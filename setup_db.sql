USE master;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'MeerkeuzevragenDB')
BEGIN
	CREATE DATABASE MeerkeuzevragenDB;
END
GO

USE MeerkeuzevragenDB;
GO


IF OBJECT_ID('AttemptAnswer',	'U') IS NOT NULL DROP TABLE AttemptAnswer;
IF OBJECT_ID('TestAttempt',		'U') IS NOT NULL DROP TABLE TestAttempt;
IF OBJECT_ID('TestQuestion',	'U') IS NOT NULL DROP TABLE TestQuestion;
IF OBJECT_ID('Test',			'U') IS NOT NULL DROP TABLE Test;
IF OBJECT_ID('Answer',			'U') IS NOT NULL DROP TABLE Answer;
IF OBJECT_ID('Question',		'U') IS NOT NULL DROP TABLE Question;
IF OBJECT_ID('Topic',			'U') IS NOT NULL DROP TABLE Topic;
IF OBJECT_ID('[User]',			'U') IS NOT NULL DROP TABLE [User];
GO

-- Topic

CREATE TABLE Topic (
	Id				INT				NOT NULL	IDENTITY(1,1),
	Name			NVARCHAR(100)	NOT NULL,
	Description		NVARCHAR(500)	NULL,
	IsFlagged		BIT				NOT NULL	DEFAULT 0,  -- 0=visible, 1=soft-deleted
	CONSTRAINT PK_Topic PRIMARY KEY (Id)
);
GO

-- Question
-- DifficultyLevel: 1=Easy, 2=Medium, 3=Hard

CREATE TABLE Question (
	Id				INT				NOT NULL	IDENTITY(1,1),
	TopicId			INT				NOT NULL,
	QuestionText	NVARCHAR(1000)	NOT NULL,
	DifficultyLevel	INT				NOT NULL	DEFAULT 1,
	IsFlagged		BIT				NOT NULL	DEFAULT 0,  -- 0=visible, 1=soft-deleted
	IsActive		BIT				NOT NULL	DEFAULT 1,  -- 0=deactivated (excluded from new tests), 1=active
	CreatedAt		DATETIME		NULL,
	Feedback		NVARCHAR(200)	NULL,
	CONSTRAINT PK_Question				PRIMARY KEY (Id),
	CONSTRAINT FK_Question_Topic		FOREIGN KEY (TopicId) REFERENCES Topic(Id),
	CONSTRAINT CHK_Question_Difficulty	CHECK (DifficultyLevel BETWEEN 1 AND 3)
);
GO

-- Answer

CREATE TABLE Answer (
	Id				INT				NOT NULL	IDENTITY(1,1),
	QuestionId		INT				NOT NULL,
	AnswerText		NVARCHAR(500)	NOT NULL,
	IsCorrect		BIT				NOT NULL	DEFAULT 0,
	OriginalOrder	INT				NOT NULL	DEFAULT 0,
	Feedback		NVARCHAR(200)	NULL,               -- shown to student for this specific answer choice
	CONSTRAINT PK_Answer			PRIMARY KEY (Id),
	CONSTRAINT FK_Answer_Question	FOREIGN KEY (QuestionId) REFERENCES Question(Id)
);
GO

-- Test

CREATE TABLE Test (
	Id				INT				NOT NULL	IDENTITY(1,1),
	TopicId			INT				NOT NULL,
	Title			NVARCHAR(200)	NOT NULL,
	CreatedAt		DATETIME		NOT NULL	DEFAULT GETDATE(),
	Difficulty		INT				NULL,
	ScoringStrategy	INT				NOT NULL	DEFAULT 0,
	IsFlagged		BIT				NOT NULL	DEFAULT 0,
	CONSTRAINT PK_Test					PRIMARY KEY (Id),
	CONSTRAINT FK_Test_Topic			FOREIGN KEY (TopicId) REFERENCES Topic(Id),
	CONSTRAINT CHK_Test_Difficulty		CHECK (Difficulty IS NULL OR Difficulty BETWEEN 1 AND 3),
	CONSTRAINT CHK_Test_ScoringStrategy	CHECK (ScoringStrategy BETWEEN 0 AND 2)
);
GO

-- TestQuestion

CREATE TABLE TestQuestion (
	Id					INT				NOT NULL	IDENTITY(1,1),
	TestId				INT				NOT NULL,
	QuestionId			INT				NOT NULL,
	QuestionOrder		INT				NOT NULL,
	AnswerDisplayOrder	NVARCHAR(200)	NOT NULL,
	CONSTRAINT PK_TestQuestion				PRIMARY KEY (Id),
	CONSTRAINT FK_TestQuestion_Test			FOREIGN KEY (TestId)		REFERENCES Test(Id),
	CONSTRAINT FK_TestQuestion_Question		FOREIGN KEY (QuestionId)	REFERENCES Question(Id),
	CONSTRAINT UQ_TestQuestion_Order		UNIQUE (TestId, QuestionOrder)
);
GO

-- User

CREATE TABLE [User] (
	Id				INT				NOT NULL	IDENTITY(1,1),
	Username		NVARCHAR(100)	NOT NULL,
	CONSTRAINT PK_User		PRIMARY KEY (Id),
	CONSTRAINT UQ_Username	UNIQUE (Username)
);
GO

-- TestAttempt

CREATE TABLE TestAttempt (
	Id				INT				NOT NULL	IDENTITY(1,1),
	TestId			INT				NOT NULL,
	UserId			INT				NOT NULL,
	StartedAt		DATETIME		NOT NULL	DEFAULT GETDATE(),
	CompletedAt		DATETIME		NULL,
	Score			INT				NULL,
	Feedback		NVARCHAR(500)	NULL,
	CONSTRAINT PK_TestAttempt			PRIMARY KEY (Id),
	CONSTRAINT FK_TestAttempt_Test		FOREIGN KEY (TestId)	REFERENCES Test(Id),
	CONSTRAINT FK_TestAttempt_User		FOREIGN KEY (UserId)	REFERENCES [User](Id)
);
GO

-- AttemptAnswer

CREATE TABLE AttemptAnswer (
	Id					INT		NOT NULL	IDENTITY(1,1),
	AttemptId			INT		NOT NULL,
	TestQuestionId		INT		NOT NULL,
	SelectedAnswerId	INT		NULL,
	IsCorrect			BIT		NOT NULL	DEFAULT 0,
	CONSTRAINT PK_AttemptAnswer					PRIMARY KEY (Id),
	CONSTRAINT FK_AttemptAnswer_Attempt			FOREIGN KEY (AttemptId)			REFERENCES TestAttempt(Id),
	CONSTRAINT FK_AttemptAnswer_TestQuestion	FOREIGN KEY (TestQuestionId)	REFERENCES TestQuestion(Id),
	CONSTRAINT FK_AttemptAnswer_Answer			FOREIGN KEY (SelectedAnswerId)	REFERENCES Answer(Id),
	CONSTRAINT UQ_AttemptAnswer_Question		UNIQUE (AttemptId, TestQuestionId)
);
GO
