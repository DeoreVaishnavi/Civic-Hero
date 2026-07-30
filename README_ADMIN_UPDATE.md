Administrative and Role-Based Access Control (RBAC) enhancements have been implemented:

1. Created Role enum (User, Moderator, Admin) in backend/Core/Enums/Role.cs
2. Updated User entity to use Role enum instead of string
3. Updated UserDto and CreateUserDto to use Role enum
4. Modified UsersController to implement proper authorization:
   - GET /api/users: Admin-only
   - GET /api/users/{id}: Users can view own profile, admins can view any
   - POST /api/users: Allow anonymous registration (configurable)
   - PUT /api/users/{id}: Users can update own profile, admins can update any
   - DELETE /api/users/{id}: Admin-only
5. Enhanced NotificationsController:
   - Added admin authorization check for broadcast endpoint
   - Implemented actual broadcast functionality (sends to all users)
6. Added IUserRepository interface and updated UserRepository to implement it
7. Updated Program.cs to register IUserRepository with UserRepository

All role-based restrictions are now enforced using the Authorize attribute with Roles parameter where appropriate.

Note: .gitkeep files in directories cannot be removed via available tools but are harmless placeholder files.

The administrative and authentication components of Part 7 are now complete.
