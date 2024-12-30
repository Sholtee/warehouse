/********************************************************************************
* 01-Auth.sql                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
CREATE TABLE `Group` 
(
  `Name` VARCHAR(255) NULL, 
  `Description` VARCHAR(1024) NULL, 
  `Roles` INT(11) NOT NULL, 
  `Id` CHAR(36) PRIMARY KEY, 
  `CreatedUtc` DATETIME NOT NULL 
); 

CREATE UNIQUE INDEX uidx_group_name ON `Group` (`Name`); 

CREATE TABLE `User` 
(
  `ClientId` VARCHAR(255) NULL, 
  `ClientSecretHash` VARCHAR(1024) NOT NULL, 
  `DeletedUtc` DATETIME NULL, 
  `Id` CHAR(36) PRIMARY KEY, 
  `CreatedUtc` DATETIME NOT NULL 
); 

CREATE UNIQUE INDEX uidx_user_clientid ON `User` (`ClientId`); 

CREATE  INDEX idx_user_deletedutc ON `User` (`DeletedUtc`); 

CREATE TABLE `UserGroup` 
(
  `UserId` CHAR(36) NOT NULL, 
  `GroupId` CHAR(36) NOT NULL, 
  `Id` CHAR(36) PRIMARY KEY, 
  `CreatedUtc` DATETIME NOT NULL, 

  CONSTRAINT `FK_UserGroup_User_UserId` FOREIGN KEY (`UserId`) REFERENCES `User` (`Id`), 

  CONSTRAINT `FK_UserGroup_Group_GroupId` FOREIGN KEY (`GroupId`) REFERENCES `Group` (`Id`) 
); 

CREATE UNIQUE INDEX uidx_usergroup_userid_groupid ON `UserGroup` (`UserId`, `GroupId`); 