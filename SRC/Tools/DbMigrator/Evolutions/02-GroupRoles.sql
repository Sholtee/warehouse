/********************************************************************************
* 02-GroupRoles.sql                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
INSERT INTO `Role` (`Name`,`Description`,`Id`,`CreatedUtc`) VALUES ('Admin',NULL,'27549ca5-68db-495c-9de6-e2c4ede15519','2024-12-14 16:32:19');
INSERT INTO `Role` (`Name`,`Description`,`Id`,`CreatedUtc`) VALUES ('User',NULL,'7da562b0-74d8-4f89-aa77-538c1e6f344c','2024-12-14 16:32:19');
INSERT INTO `Group` (`Name`,`Description`,`Id`,`CreatedUtc`) VALUES ('Admins','Administrators group','f36cd8ea-e209-4d64-885e-1357f1cd2f2a','2024-12-14 16:32:19');
INSERT INTO `GroupRole` (`GroupId`,`RoleId`,`Id`,`CreatedUtc`) VALUES ('f36cd8ea-e209-4d64-885e-1357f1cd2f2a','27549ca5-68db-495c-9de6-e2c4ede15519','991c5f05-d741-4945-96bd-a23118720b23','2024-12-14 16:32:19');
INSERT INTO `GroupRole` (`GroupId`,`RoleId`,`Id`,`CreatedUtc`) VALUES ('f36cd8ea-e209-4d64-885e-1357f1cd2f2a','7da562b0-74d8-4f89-aa77-538c1e6f344c','ad489007-ccff-4db5-bb6f-1d5f268c56a7','2024-12-14 16:32:19');
INSERT INTO `Group` (`Name`,`Description`,`Id`,`CreatedUtc`) VALUES ('Users','Users group','cef82ecf-988f-4fa3-aab2-add040f862fc','2024-12-14 16:32:19');
INSERT INTO `GroupRole` (`GroupId`,`RoleId`,`Id`,`CreatedUtc`) VALUES ('cef82ecf-988f-4fa3-aab2-add040f862fc','7da562b0-74d8-4f89-aa77-538c1e6f344c','c0d77526-305a-4810-85a6-e0ebae2a6dc9','2024-12-14 16:32:19');