# User Management Features

Focused reference for the current user-management implementation.

## Current Capabilities

- admin-only access to the `/users` page
- create users with role selection
- client-side confirm-password validation on create
- update user roles
- delete users
- reset passwords for existing users

## Backend Behavior

Relevant API endpoints:

- `GET /api/users`
- `POST /api/users`
- `PUT /api/users/{id}/role`
- `PUT /api/users/{id}/password`
- `DELETE /api/users/{id}`

Current rules:

- all user-management endpoints require the `UsersManage` policy
- only `Admin` is allowed
- password reset requires `newPassword` and `confirmPassword`
- reset returns `400` for mismatch or short passwords
- self-role updates return a refreshed auth payload

## Frontend Behavior

The current `UsersPage` supports:

- password confirmation on create
- independent show/hide toggles for password fields
- role editing
- delete confirmation
- reset-password flow for an existing selected user

## Notes

- self-registration through `/api/auth/register` is disabled and returns `403`
- user creation is handled by admins through `/api/users`, not by public registration

## Related Docs

- [README.md](./README.md)
- [docs/Test-Documents/API_TEST_PLAN.md](./docs/Test-Documents/API_TEST_PLAN.md)
- [docs/Test-Documents/TESTING_OVERVIEW.md](./docs/Test-Documents/TESTING_OVERVIEW.md)
