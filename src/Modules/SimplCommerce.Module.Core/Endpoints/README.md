# Core/Endpoints

Phase 3 home for Minimal API endpoint groups owned by the Core module
(account/auth, users, customer groups, addresses, widgets, themes,
countries, etc.). Each group will be a static class exposing a
`Map<Group>Endpoints(this IEndpointRouteBuilder app)` method that the
ApiService composition root calls via `MapCoreEndpoints()`.

For Phase 2 the existing AngularJS-facing controllers under
`Areas/Core/Controllers/` are still authoritative — Phase 3 replaces
each one with a Minimal API endpoint group here.
