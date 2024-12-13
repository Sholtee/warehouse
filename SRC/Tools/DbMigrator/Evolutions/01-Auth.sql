CREATE TABLE `Group` 
(
  `GroupId` VARCHAR(255) PRIMARY KEY, 
  `Description` VARCHAR(1024) NULL, 
  `CreatedUtc` DATETIME NOT NULL, 
  `DeletedUtc` DATETIME NULL 
); 

CREATE TABLE `Role` 
(
  `RoleId` VARCHAR(255) PRIMARY KEY, 
  `Description` VARCHAR(1024) NULL, 
  `CreatedUtc` DATETIME NOT NULL, 
  `DeletedUtc` DATETIME NULL 
); 

CREATE TABLE `GroupRole` 
(
  `GroupId` VARCHAR(255) PRIMARY KEY, 
  `RoleId` VARCHAR(255) NOT NULL, 
  `CreatedUtc` DATETIME NOT NULL, 
  `DeletedUtc` DATETIME NULL, 

  CONSTRAINT `FK_GroupRole_Group_GroupId` FOREIGN KEY (`GroupId`) REFERENCES `Group` (`GroupId`), 

  CONSTRAINT `FK_GroupRole_Role_RoleId` FOREIGN KEY (`RoleId`) REFERENCES `Role` (`RoleId`) 
); 

CREATE TABLE `User` 
(
  `ClientId` VARCHAR(255) PRIMARY KEY, 
  `ClientSecretHash` VARCHAR(1024) NOT NULL, 
  `CreatedUtc` DATETIME NOT NULL, 
  `DeletedUtc` DATETIME NULL 
); 

CREATE TABLE `UserGroup` 
(
  `ClientId` VARCHAR(255) PRIMARY KEY, 
  `GroupId` VARCHAR(255) NOT NULL, 
  `CreatedUtc` DATETIME NOT NULL, 
  `DeletedUtc` DATETIME NULL, 

  CONSTRAINT `FK_UserGroup_User_ClientId` FOREIGN KEY (`ClientId`) REFERENCES `User` (`ClientId`), 

  CONSTRAINT `FK_UserGroup_Group_GroupId` FOREIGN KEY (`GroupId`) REFERENCES `Group` (`GroupId`) 
);