-- Insert test tours for replacement testing
INSERT INTO tours."Tours" ("Id", "AuthorId", "Name", "Description", "Difficulty", "Category", "Price", "Date", "State")
VALUES 
    (-1, -11, 'Test Tour 1', 'Description 1', 3, 2, 1000, '2026-03-15 10:00:00', 1),
    (-2, -11, 'Test Tour 2', 'Description 2', 2, 1, 800, '2026-04-20 10:00:00', 1),
    (-3, -12, 'Test Tour 3', 'Description 3', 4, 3, 1500, '2026-05-25 10:00:00', 1);