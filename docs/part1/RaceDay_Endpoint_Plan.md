# RaceDay API Endpoint Plan
## Part 1 – System Planning and Database

The API uses `/api` as the base path. JSON is used for request and response bodies. Protected endpoints require an access token. The API is designed around resources and standard HTTP methods. GET is used for reading data, POST for creating resources, PUT for replacing/updating a resource, and DELETE for removing a resource (MDN Web Docs, 2025a).

| HTTP Method | Route | Description | Role Required | Request Body (if any) | Expected Response |
|---|---|---|---|---|---|
| POST | /api/auth/register | Create a new participant account. | Public | {"firstName":"Anele","lastName":"Mokoena","email":"anele@example.com","password":"***"} | 201 Created; user details and access token. |
| POST | /api/auth/login | Log a user in and return an access token. | Public | {"email":"anele@example.com","password":"***"} | 200 OK; token, user ID and role. |
| GET | /api/profile | Return the logged-in user's profile. | Participant / Organiser | None | 200 OK; profile details. |
| PUT | /api/profile | Update the logged-in user's profile. | Participant / Organiser | {"firstName":"Anele","lastName":"Mokoena"} | 200 OK; updated profile. |
| GET | /api/categories | List event categories. | Public | None | 200 OK; array of categories. |
| POST | /api/categories | Create a new event category. | Organiser | {"categoryName":"10 KM Road Race","description":"Road running event"} | 201 Created; category object. |
| PUT | /api/categories/{id} | Edit an event category. | Organiser | {"categoryName":"10 KM Run","description":"Updated description"} | 200 OK; updated category. |
| DELETE | /api/categories/{id} | Delete a category when it is not in use. | Organiser | None | 204 No Content; 404 if not found; 409 if still used. |
| GET | /api/events | List upcoming events, with optional category filter. | Public | Query: ?categoryId=1 | 200 OK; array of event summaries. |
| GET | /api/events/{id} | View one event and its route/category details. | Public | None | 200 OK; event details; 404 if not found. |
| POST | /api/events | Create a new event. | Organiser | {"eventName":"Pretoria Spring Run","eventDate":"2026-10-10","startTime":"07:00","location":"Pretoria","capacity":500,"categoryId":1,"routeId":1} | 201 Created; event object. |
| PUT | /api/events/{id} | Update an event owned by the organiser. | Organiser | {"eventName":"Pretoria Spring Run","capacity":550} | 200 OK; updated event. |
| DELETE | /api/events/{id} | Delete an event owned by the organiser. | Organiser | None | 204 No Content; 404 if not found. |
| GET | /api/events/{id}/enrolments | View all enrolments for an organiser's event. | Organiser | None | 200 OK; participant enrolment list. |
| POST | /api/events/{id}/enrolments | Enrol the logged-in participant in an event. | Participant | {"bibNumber":101} | 201 Created; enrolment object; 409 if already enrolled/full. |
| GET | /api/enrolments/me | View the logged-in participant's enrolments. | Participant | None | 200 OK; participant's enrolment list. |
| GET | /api/enrolments/{id} | View one enrolment belonging to the logged-in participant. | Participant | None | 200 OK; enrolment details; 403 if it belongs to another user. |
| DELETE | /api/enrolments/{id} | Cancel the logged-in participant's enrolment. | Participant | None | 204 No Content; 404 if not found. |
| GET | /api/results/me | View the logged-in participant's results. | Participant | None | 200 OK; result history. |
| POST | /api/enrolments/{id}/result | Record a result for an enrolment. | Organiser | {"finishTime":"01:02:34","position":12} | 201 Created; result object. |
| PUT | /api/results/{id} | Correct or update a recorded result. | Organiser | {"finishTime":"01:02:20","position":11} | 200 OK; updated result. |
| GET | /api/events/{id}/results | View results for an event. | Organiser | None | 200 OK; results ordered by position. |

### API design decisions

1. Authentication is separated from the other resources so that registration and login are easy to understand.
2. Organiser-only endpoints are protected at API level. The MVC interface in Part 3 will also hide organiser features from participants, but the API will remain the main security control. This follows the principle that protected REST endpoints should check authorisation themselves rather than relying only on the client interface (OWASP Foundation, 2026).
3. The participant's own resources use `/me` where possible. This reduces the chance of accidentally exposing another participant's private information.
4. A `409 Conflict` response is planned for business-rule problems such as duplicate enrolments, full events or trying to delete a category that is still being used.
5. A successful creation uses `201 Created`, while successful reads/updates normally use `200 OK` and successful deletes use `204 No Content` (MDN Web Docs, 2025b).
6. The endpoint plan is deliberately written before Part 2 so that the implementation can be checked against the original design.
