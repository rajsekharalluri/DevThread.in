# Development

## Backend

The API uses the content directory at the repository root when started with the project profile. It exposes:

- `GET /api/topics`
- `GET /api/topics/{category}/{slug}`
- `GET /api/categories`
- `GET /api/search?q=async`
- `GET /api/content/validate`

## Frontend

The Angular development server runs on port 4200 and calls `/api` through the configured development proxy when one is added for a local environment. The production API can be hosted behind the same origin.

## Browser learning state

The store key is `developer-academy.learning-state.v1`. Each topic stores its current status and update timestamp. The state is intentionally not cleared on reload or application restart. A later settings page can add an explicit “clear learning data” action.

## Testing expectations

Backend tests should cover metadata parsing, topic resolution, search, and validation. Frontend tests should cover status persistence, route rendering, search rendering, and bookmark toggling. Avoid tests that only assert framework-generated markup.
