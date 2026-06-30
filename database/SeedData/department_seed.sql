USE civicherodb;
INSERT INTO Departments (Name, Description, CreatedAt)
VALUES
('Roads', 'Handles road repair and pothole complaints', UTC_TIMESTAMP()),
('Water', 'Handles water supply and pipeline complaints', UTC_TIMESTAMP()),
('Street Lighting', 'Handles street light issues', UTC_TIMESTAMP()),
('Garbage Collection', 'Handles waste and garbage complaints', UTC_TIMESTAMP()),
('Parks', 'Handles park and public garden maintenance', UTC_TIMESTAMP());