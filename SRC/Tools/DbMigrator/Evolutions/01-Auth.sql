CREATE TABLE `Group` 
(
  `Name` VARCHAR(255) NULL, 
  `Description` VARCHAR(1024) NULL, 
  `Id` CHAR(36) PRIMARY KEY, 
  `CreatedUtc` DATETIME NOT NULL, 
  `DeletedUtc` DATETIME NULL 
); 

CREATE UNIQUE INDEX uidx_group_name ON `Group` (`Name`); 

CREATE  INDEX idx_group_deletedutc ON `Group` (`DeletedUtc`); 

CREATE TABLE `Role` 
(
  `Name` VARCHAR(255) NULL, 
  `Description` VARCHAR(1024) NULL, 
  `Id` CHAR(36) PRIMARY KEY, 
  `CreatedUtc` DATETIME NOT NULL, 
  `DeletedUtc` DATETIME NULL 
); 

CREATE UNIQUE INDEX uidx_role_name ON `Role` (`Name`); 

CREATE  INDEX idx_role_deletedutc ON `Role` (`DeletedUtc`); 

CREATE TABLE `GroupRole` 
(
  `GroupId` CHAR(36) NOT NULL, 
  `RoleId` CHAR(36) NOT NULL, 
  `Id` CHAR(36) PRIMARY KEY, 
  `CreatedUtc` DATETIME NOT NULL, 
  `DeletedUtc` DATETIME NULL, 

  CONSTRAINT `FK_GroupRole_Group_GroupId` FOREIGN KEY (`GroupId`) REFERENCES `Group` (`Id`), 

  CONSTRAINT `FK_GroupRole_Role_RoleId` FOREIGN KEY (`RoleId`) REFERENCES `Role` (`Id`) 
); 

CREATE  INDEX idx_grouprole_deletedutc ON `GroupRole` (`DeletedUtc`); 

CREATE UNIQUE INDEX uidx_grouprole_groupid_roleid ON `GroupRole` (`GroupId`, `RoleId`); 

CREATE TABLE `User` 
(
  `ClientId` VARCHAR(255) NULL, 
  `ClientSecretHash` VARCHAR(1024) NOT NULL, 
  `Id` CHAR(36) PRIMARY KEY, 
  `CreatedUtc` DATETIME NOT NULL, 
  `DeletedUtc` DATETIME NULL 
); 

CREATE UNIQUE INDEX uidx_user_clientid ON `User` (`ClientId`); 

CREATE  INDEX idx_user_deletedutc ON `User` (`DeletedUtc`); 

CREATE TABLE `UserGroup` 
(
  `ClientId` CHAR(36) NOT NULL, 
  `GroupId` CHAR(36) NOT NULL, 
  `Id` CHAR(36) PRIMARY KEY, 
  `CreatedUtc` DATETIME NOT NULL, 
  `DeletedUtc` DATETIME NULL, 

  CONSTRAINT `FK_UserGroup_User_ClientId` FOREIGN KEY (`ClientId`) REFERENCES `User` (`Id`), 

  CONSTRAINT `FK_UserGroup_Group_GroupId` FOREIGN KEY (`GroupId`) REFERENCES `Group` (`Id`) 
); 

CREATE  INDEX idx_usergroup_deletedutc ON `UserGroup` (`DeletedUtc`); 

CREATE UNIQUE INDEX uidx_usergroup_clientid_groupid ON `UserGroup` (`ClientId`, `GroupId`);