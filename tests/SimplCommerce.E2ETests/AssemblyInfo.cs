// Tag every NUnit test in this assembly with the existing "RequiresDocker"
// category so the main CI build-test job filter (Category!=RequiresDocker)
// skips them. The full Aspire stack (SQL Server, Redis, Storage emulator,
// MailPit, Seq) is brought up via Docker by the integration-tests job — same
// gate as the existing Testcontainers-backed integration suite.
//
// To run locally with the stack already up:
//   dotnet test tests/SimplCommerce.E2ETests --filter "Category=RequiresDocker"
[assembly: NUnit.Framework.Category("RequiresDocker")]
