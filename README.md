# FoundationKit Project

## 📌 Overview
This project implements a modular **User & Role Management system** built on .NET 8 with Entity Framework Core and PostgreSQL.  
It follows a **clean layered architecture** with Entities, DTOs, Repositories, Services, and Endpoints.

---

## 🏗 Architecture Breakdown

- **Entities (`User`, `Role`, `UserRole`)**
  - Core domain objects, mapped to database tables via EF Core.
  - Support soft delete (`IsDeleted`) and auditing (`CreatedDate`, `UpdatedDate`).

- **DTOs (`UserListDto`, `UserDetailDto`, `UserCreateDto`, `UserUpdateDto`)**
  - Define contracts for API input/output.
  - Prevent direct entity exposure.

- **Repositories (`EfBaseRepository`, `UserRepository`, `RoleRepository`)**
  - Encapsulate EF Core queries with paging, filtering, search, and sorting.
  - `UserRepository` ensures roles are eagerly loaded (`Include(u => u.UserRoles).ThenInclude(r => r.Role)`).

- **Services (`BaseService`, `UserService`, `RoleService`)**
  - Contain business logic (e.g., validating email uniqueness, managing role assignments).
  - Extend generic `BaseService` with user-specific rules.

- **Endpoints (`UserManagementEndpoints`, `RolesEndpoints`)**
  - Expose RESTful API endpoints with pagination, search, and sorting.

---

## 📊 ASCII Dependency Diagram

```
+-----------+      +-----------+      +----------------+      +--------------+      +-------------+
| Entities  | ---> |   DTOs    | ---> | Repositories   | ---> |   Services   | ---> |  Endpoints  |
+-----------+      +-----------+      +----------------+      +--------------+      +-------------+
   User            UserDto(s)          UserRepository          UserService          UserManagement
   Role                                RoleRepository          RoleService          RoleManagement
```

---

## 🛠 Technologies Used

- **.NET 8 / C#**
- **Entity Framework Core**
- **PostgreSQL**
- **Minimal APIs**
- **Swagger / OpenAPI**

---

## 🚀 Getting Started

1. Update `appsettings.json` with your PostgreSQL connection string.
2. Run EF Core migrations:
   ```bash
   dotnet ef database update
   ```
3. Run the project:
   ```bash
   dotnet run
   ```
4. Open Swagger UI at `https://localhost:5001/swagger`

---

## 📡 API Endpoints

### Users
- `GET    /users?page=1&pageSize=20&search=abc&sort=firstName`
- `GET    /users/{id}`
- `POST   /users`
- `PUT    /users/{id}`
- `DELETE /users/{id}`

### Roles
- `GET    /roles`
- `POST   /roles`
- `DELETE /roles/{id}`

---

## ✅ Testing

- Tested user creation, role assignment, updates, and soft deletes.
- Verified search + pagination + sorting.
- Swagger confirms working endpoints.

---

## 📌 Next Steps

- Add authentication & authorization (JWT / Azure AD).
- Implement audit logging service.
- Add integration tests for repositories & services.
- Extend role-based access in endpoints.

---
