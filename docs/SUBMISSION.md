# Part 2 submission checklist

## Check the app

- Open RaceDay.sln and run it.
- Register an Organiser and a Participant.
- Create a category, route and event.
- Enrol a Participant and try a duplicate entry.
- Check that a Participant cannot create events.
- Check that another Organiser cannot change your event.
- Record a result for an event that has started, then view it as the Participant.
- Run all tests in Test Explorer.

This version uses InMemory as requested. Data is lost when the API stops. 

## GitHub

Complete the required 20 meaningful commits through real work in your own account. No commit history has been created for you.

The workflow must be in the repository root under .github/workflows. Push the code, wait for GitHub Actions to pass, and add a genuine screenshot to README.md.

## Video

Record an unlisted YouTube video showing:

1. The purpose of RaceDay and its two roles.
2. A model, the context and Program.cs.
3. A controller's GET, POST, PUT and DELETE methods.
4. Registration, login and logout.
5. Event creation and Participant enrolment.
6. Rejected duplicate enrolments and wrong-role requests.
7. Results captured by an Organiser and viewed by the Participant.
8. Password hashing and session checks.
9. A unit test explained using Arrange, Act and Assert.
10. The successful GitHub Actions run.

Add the video link and CI screenshot to README.md, then submit the repository link on ARC.

