# Test Delivery Checklist

Use this checklist before handing off the current test/documentation package.

## Documentation

- [ ] `docs/Test-Documents/TESTING_OVERVIEW.md` is treated as the canonical test-doc landing page
- [ ] active guides do not refer to missing sprint-era files
- [ ] archived reports are clearly historical
- [ ] validated test counts are current in active docs

## Backend validation

- [ ] `dotnet test backend/HSMS.Tests/HSMS.Tests.csproj --no-restore`
- [ ] DB-backed integration expectations are documented if `HSMS_TEST_CONNECTION_STRING` is required

## API validation

- [ ] backend API is started before API test execution
- [ ] `dotnet test backend/HSMS.ApiTests/HSMS.ApiTests.csproj --no-restore`
- [ ] current auth behavior is documented, including disabled self-registration

## Frontend validation

- [ ] `npm test` passes in `frontend/HWSMS_UI`
- [ ] `npm run build` passes in `frontend/HWSMS_UI`
- [ ] route access descriptions match the current app

## Artifact validation

- [ ] Postman collection path is correct
- [ ] JMeter artifact paths are correct
- [ ] generator script paths are correct

## Final review

- [ ] no active Markdown file points to a missing Markdown target
- [ ] no active test doc still presents the old 47-test sprint-only state as current
- [ ] redundant active docs have been removed or merged intentionally

## Related Docs

- [TEST_EXECUTION_GUIDE.md](./TEST_EXECUTION_GUIDE.md)
- [../TESTING_OVERVIEW.md](../TESTING_OVERVIEW.md)
