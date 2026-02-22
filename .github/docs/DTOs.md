# Why Use DTOs?

## 1. Separation of Concerns

DTOs decouple:

- Database structure (EF entities)
- Business logic (Domain models)
- API contract (External representation)

This prevents internal changes from breaking external consumers.

---

## 2. Security & Controlled Exposure

DTOs ensure only intended fields are exposed.

Prevents:

- Leaking sensitive data (e.g., PasswordHash, IsAdmin)
- Overposting attacks
- Accidental navigation property serialization

DTOs define exactly what the outside world sees.

---

## 3. Performance Optimization

DTOs allow:

- Selecting only required fields in queries
- Avoiding full entity graph loading
- Reducing serialization size

Improves API throughput and scalability.

---

## 4. API Stability & Versioning

DTOs allow versioned contracts:

- `UserDtoV1`
- `UserDtoV2`

Internal domain and database models remain unaffected by API evolution.

---

## 5. Validation Boundary

DTOs are the correct place for:

- Input validation attributes
- API-level constraints

Keeps domain logic clean and focused on business rules.

---

## Core Architectural Principle

> The API contract should not be your database schema.

DTOs create a protective boundary between:

- External clients
- Application logic
- Domain logic
- Infrastructure

---

## Recommended Solution Structure (Scalable)

```plaintext
MyApp.sln
 └── src
      ├── MyApp.API
      ├── MyApp.Application
      ├── MyApp.Domain
      └── MyApp.Infrastructure
```

---

## Layer Responsibilities

### 1. Domain (Core Business Layer)

**Contains:**

- Domain entities
- Value objects
- Business rules
- Domain interfaces

**Rules:**

- No EF Core
- No ASP.NET
- No infrastructure dependencies

Pure business logic only.

---

### 2. Application (Use Case Layer)

**Contains:**

- DTOs
- Commands / Queries
- Application services
- Validation logic
- Mapping profiles

**Depends on:** Domain
**Does NOT depend on:** Infrastructure

This layer orchestrates use cases.

---

### 3. Infrastructure (Technical Layer)

**Contains:**

- EF DbContext
- Entity configurations
- Repository implementations
- External service integrations
- Migrations

#### Implements interfaces defined in Domain/Application

Handles technical details only.

---

### 4. API (Presentation Layer)

**Contains:**

- Controllers
- Middleware
- HTTP configuration

**Depends on:** Application
**Works only with DTOs**

No business logic or EF access.

---

## Dependency Direction

```plaintext
API → Application → Domain
Infrastructure → Domain
```

Never reverse this direction.

---

## Entity Strategy

### Common Practical Approach

Use Domain entities and let EF map to them in Infrastructure.

Suitable for most business applications.

### Advanced Approach (Optional)

Separate persistence models from domain models.

Useful for:

- Complex domains
- Long-lived enterprise systems

Often unnecessary unless domain complexity justifies it.

---

## Key Takeaways

- DTOs protect your API boundary.
- Domain contains business rules — nothing technical.
- Infrastructure contains technical implementations.
- Application orchestrates use cases.
- API exposes DTOs only.
- Dependencies flow inward.

---

## Architectural Outcome

Using DTOs and layered structure results in:

- Clear boundaries
- Strong security posture
- Easier testing
- Better maintainability
- Controlled API evolution
- Long-term scalability

Design for change, not just for today.
