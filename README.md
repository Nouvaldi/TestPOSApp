## .Net Core API using Swagger

JWT (JSON Web Token) Authentication on API:
- Implement JWT authentication on API to secure access to Items, POS, and reports menus.
- JWT tokens are generated after a user successfully logs in and are used on every protected request.
- Use JWT token in Authorization header with Bearer <token> format for every protected request.

API Endpoint:
- POST/api/auth/register: Register new user.
- POST/api/auth/login: login user.
- POST/api/items: Add new items, including image uploads.
- GET/api/items: Get a list of items.
- PUT /api/items/{id}: Edit an existing item.
- DELETE/api/items/{id}: Deletes an item by ID.
- POST/api/pos/transactions: Process sales transactions.
- GET /api/pos/transactions: Get transaction history.
- GET /api/pos/reports: Get POS transaction reports.
- GET/api/items/stock: Get a report on the stock availability of items.
