drop schema if exists dødeduer cascade;
create schema if not exists dødeduer;

-- Admin table
create table dødeduer.admin
(
    id           text primary key         not null,
    name         text                     not null,
    email        text                     not null unique,
    phoneNumber  text                     not null,
    passwordHash text                     not null,
    createdAt    timestamp with time zone not null,
    isDeleted    boolean                  not null default false
);

-- Player table
create table dødeduer.player
(
    id           text primary key         not null,
    name         text                     not null,
    email        text                     not null unique,
    phoneNumber  text                     not null,
    passwordHash text                     not null,
    createdAt    timestamp with time zone not null,
    funds        numeric(10, 2)           not null,
    isDeleted    boolean                  not null default false
);


-- Board table
create table dødeduer.boards
(
    id           text primary key         not null,
    weekNumber   int                      not null,
    year         int                      not null,
    startDate    timestamp with time zone not null,
    endDate      timestamp with time zone not null,
    isActive     boolean                  not null default false,
    createdAt    timestamp with time zone not null
);

-- Player's purchased boards
create table dødeduer.playerboards
(
    id           text primary key         not null,
    playerId     text                     not null references dødeduer.player(id),
    boardId      text                     not null references dødeduer.boards(id),
    createdAt    timestamp with time zone not null,
    isWinner     boolean                  not null default false
);

-- Numbers chosen by player for their board (5-8 numbers between 1-16)
create table dødeduer.playerboardnumbers
(
    id              text primary key         not null,
    playerBoardId   text                     not null references dødeduer.playerboards(id),
    selectedNumber  int                      not null check (selectedNumber >= 1 and selectedNumber <= 16)
);

-- 3 winning numbers drawn by admin at end of week
create table dødeduer.drawnnumbers
(
    id              text primary key         not null,
    boardId         text                     not null references dødeduer.boards(id),
    drawnNumber     int                      not null check (drawnNumber >= 1 and drawnNumber <= 16),
    drawnAt         timestamp with time zone not null,
    drawnBy         text                     not null references dødeduer.admin(id)
);
