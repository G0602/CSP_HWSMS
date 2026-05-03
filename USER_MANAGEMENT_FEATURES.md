# User Management Features - Updated Documentation

## Overview

The User Management page has been enhanced with advanced password management features and improved UI alignment. This document describes the new features, API changes, and testing requirements.

---

## New Features

### 1. Password Confirmation on User Creation

**Feature:** When creating a new user, admins must now confirm the password by entering it twice.

- **Frontend Changes:**
  - Added "Confirm Password" input field with password visibility toggle
  - Both password fields have independent Show/Hide toggles matching the login page style
  - Client-side validation ensures passwords match before submission
  - Clear error message if passwords don't match: "Passwords do not match."

- **Backend Changes:**
  - No changes required (backend was already strict about password requirements)
  - Password validation: Minimum 8 characters enforced on API

- **Testing:**
  - ✅ Test Case 13.1-13.6: Positive user creation scenarios
  - ✅ Test Case 13.10-13.14: Negative user creation scenarios including password validation

### 2. Password Reset for Existing Users

**Feature:** Admins can now reset passwords for any existing user with confirmation.

- **Frontend Changes:**
  - Added "Reset Password" button in user actions table
  - New modal/form for password reset with:
    - User identification (shows which user's password is being reset)
    - New Password field with Show/Hide toggle
    - Confirm Password field with Show/Hide toggle
    - Cancel button to close the form
  - Form validation before submission
  - Success/error feedback messages

- **Backend Changes:**
  - **New DTO:** `PasswordResetDTO` with `NewPassword` and `ConfirmPassword` fields
  - **New Endpoint:** `PUT /api/users/{id}/password`
  - **Implementation:** `UpdatePasswordAsync()` method in `IUserRepository` and `UserRepository`
  - Validation:
    - Password minimum 8 characters
    - Passwords must match
    - Returns 400 Bad Request for validation errors
    - Returns 404 Not Found if user doesn't exist
    - Returns 200 OK on success

- **Testing:**
  - ✅ Test Case 13.21-13.22: Positive password reset scenarios
  - ✅ Test Case 13.23-13.26: Negative password reset scenarios

### 3. Password Visibility Toggle

**Feature:** Both create user and password reset forms now have Show/Hide toggles for passwords.

- **Implementation Details:**
  - Uses same styling and interaction pattern as login page
  - Toggle button displays "Show" or "Hide" text
  - Independent toggles for password and confirm password fields
  - Styled with primary brand color (#1f6b8c)

---

## API Changes

### New Endpoint: Reset User Password

```
PUT /api/users/{id}/password
```

**Request Body:**
```json
{
  "newPassword": "string (minimum 8 characters)",
  "confirmPassword": "string (must match newPassword)"
}
```

**Success Response (200 OK):**
```json
{
  "message": "Password reset successfully."
}
```

**Error Responses:**
- `400 Bad Request` - Password validation failed or passwords don't match
- `404 Not Found` - User not found
- `401 Unauthorized` - Not authenticated
- `403 Forbidden` - Insufficient permissions (non-admin)

**Authorization:**
- Requires: Admin role (AuthPolicies.UsersManage)
- Can reset any user's password

---

## Database Changes

No database schema changes required. Password reset uses existing `PasswordHash` column in Users table.

---

## Service Layer Changes

### IUserRepository Interface

Added method:
```csharp
Task<bool> UpdatePasswordAsync(int id, string passwordHash);
```

### UserRepository Implementation

New method implementation:
```csharp
public async Task<bool> UpdatePasswordAsync(int id, string passwordHash)
{
    // Updates Users.PasswordHash for specified user ID
    // Returns true if successful, false if user not found
}
```

---

## Frontend Component Changes

### UsersPage.tsx State Management

New state variables added:
- `confirmPassword` - Tracks confirm password input during user creation
- `showPassword` - Controls password field visibility in create form
- `showConfirmPassword` - Controls confirm password field visibility in create form
- `resetPasswordUserId` - Tracks which user's password is being reset
- `resetPasswordForm` - Tracks reset password form inputs
- `showResetPassword` - Controls password visibility in reset form
- `showResetConfirmPassword` - Controls confirm password visibility in reset form
- `resettingPasswordUserId` - Tracks loading state during password reset

### New Form Validation

**Create User Form:**
- Existing validation: username required, password ≥ 8 characters
- **New validation:** Password matches confirm password

**Reset Password Form:**
- Password ≥ 8 characters
- Passwords must match

---

## Test Coverage

### Backend API Tests (UsersApiTests.cs)

| Test Case | Scenario | Expected Result |
|-----------|----------|-----------------|
| 13.21 | Reset with valid matching passwords | 200 OK |
| 13.22 | Reset response validation | 200 OK with success message |
| 13.23 | Reset with mismatched passwords | 400 Bad Request |
| 13.24 | Reset with short password | 400 Bad Request |
| 13.25 | Reset non-existent user | 404 Not Found |
| 13.26 | Reset with empty password | 400 Bad Request |

**Total Coverage:** 26+ test cases (previously 20)

### Integration Tests (UserManagementIntegrationTests.cs)

Added Story S3-US-09: Password Reset

| Test Case | Scenario |
|-----------|----------|
| UpdatePasswordAsync_Should_Persist_New_Password_Hash | Password persists to database |
| UpdatePasswordAsync_Should_Enforce_Hash_Format | Password stored as bcrypt hash |
| UpdatePasswordAsync_For_NonExistent_User_Should_Return_False | Invalid user handling |

---

## Frontend Test Coverage

### Form Validation Tests

Recommended test cases for frontend:

1. **Create User Form:**
   - Password and confirm match → Success
   - Password and confirm don't match → Error message
   - Password toggle shows/hides text
   - Confirm password toggle shows/hides text

2. **Reset Password Form:**
   - New password and confirm match → Success
   - New password and confirm don't match → Error message
   - Passwords too short → Error message
   - Close button dismisses form
   - Reset button shows loading state

---

## UI Layout Changes

### Page Structure

The User Management page now uses a 2-column layout:

1. **Left Column (33%):** Create User Form
   - Username input
   - Password input with toggle
   - Confirm Password input with toggle
   - Role selector
   - Create button

2. **Right Column (66%):** Users Table
   - Displays all users with pagination
   - Edit role button
   - Reset Password button (NEW)
   - Delete button

3. **Below Main Content:** Reset Password Section (when active)
   - Only visible when "Reset Password" button clicked
   - 2-column form on desktop
   - Aligned password inputs
   - Cancel and Reset buttons

---

## Backward Compatibility

✅ **Fully Backward Compatible**

- Existing API functionality unchanged
- New password reset endpoint is additive (doesn't modify existing behavior)
- Frontend remains compatible with older browser versions
- No breaking changes to existing user workflows

---

## Documentation Updates

Updated files:
- `HSMS.ApiTests/README.md` - Added Member 13 coverage
- `docs/Test-Documents/API_TEST_PLAN.md` - Added password reset endpoint specs
- `backend/HSMS.ApiTests/Users/UsersApiTests.cs` - Added 6 new tests
- `backend/HSMS.Tests/Integration/Repositories/UserManagementIntegrationTests.cs` - Added 3 new integration tests
- `backend/HSMS.ApiTests/Helpers/ApiTestConstants.cs` - Added UserPassword endpoint
- `frontend/HWSMS_UI/src/pages/UsersPage.tsx` - Enhanced with new features

---

## Deployment Checklist

- [ ] Backend API built and deployed
- [ ] Database migrations applied (if any)
- [ ] Frontend built and deployed
- [ ] API tests pass (26+ tests in UsersApiTests)
- [ ] Integration tests pass (11+ in UserManagementIntegrationTests)
- [ ] Manual testing of create user flow
- [ ] Manual testing of password reset flow
- [ ] Verify password toggle functionality
- [ ] Test with different roles (Admin access only)
- [ ] Verify error messages display correctly

---

## Support and Troubleshooting

### Password Reset Not Working

1. Verify user is Admin role
2. Check API endpoint: `PUT /api/users/{userId}/password`
3. Confirm password JSON matches expected format
4. Check backend logs for validation errors

### Validation Errors

- "Password must be at least 8 characters" → Increase password length
- "Passwords do not match" → Ensure both password fields are identical
- "User not found" → Verify user ID is correct and user still exists

---

## Future Enhancements

Potential improvements:
- Email notification when password is reset
- Password history tracking
- Forced password change on first login
- Password complexity requirements
- Two-factor authentication
- Session management dashboard

---

## References

- API Specification: `/docs/Test-Documents/API_TEST_PLAN.md`
- Frontend Component: `/frontend/HWSMS_UI/src/pages/UsersPage.tsx`
- Backend Service: `/backend/HSMS.API/Controllers/UsersController.cs`
- Tests: `/backend/HSMS.ApiTests/Users/UsersApiTests.cs`
