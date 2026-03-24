# Registration Page — Frontend Rebuild From Zero

## Summary

Wipe the existing React frontend (`fasternfasterapp/src/`) and rebuild from scratch, starting with the Registration page. The page structure follows the Figma wireframe: a centered login/signup form with options to log in, continue anonymously, or sign up. Styling is original — clean, minimalistic, and visually polished — not bound to the Figma wireframe aesthetic.

## Goals

- Remove all existing files in `fasternfasterapp/src/` and rebuild from zero.
- Implement the Registration page with a centered form and a **3-tab switcher** at the top:
  - **Tab buttons**: "Log in" | "Sign up" | "Anonymous" — sticky to the top of the form, visually indicating the active mode.
  - **Log in mode**: login field + password field + submit button.
  - **Sign up mode**: login field + nick (display name) field + password field + confirm password field + submit button.
  - **Anonymous mode**: display name field + submit button (enter a name and go).
- Apply original minimalistic styling — modern, clean, visually appealing.
- Keep the app shell (public/, package.json, config files) intact — only replace `src/` contents.

## Non-Goals

- No backend integration — form submissions are non-functional (no API calls).
- No actual authentication logic.
- No routing to other pages yet.
- No mobile-responsive design (desktop-first).
- No form validation beyond basic empty-field checks.

## User Experience

1. User opens the app and sees a clean, centered registration form.
2. At the top of the form: 3 tab buttons — "Log in", "Sign up", "Anonymous". Default active tab: "Anonymous".
3. Switching tabs updates the form fields below:
   - **Log in**: login + password + "Log in" submit button.
   - **Sign up**: login + nick + password + confirm password + "Sign up" submit button.
   - **Anonymous**: display name + "Play" submit button.
4. User fills in the fields and clicks the submit button for their chosen mode.

## Edge Cases

- **Empty field submission**: submit should not trigger with empty required fields (login/password for log in, login/nick/password for sign up, display name for anonymous).
- **Tab switching with filled fields**: preserve input values when switching tabs (e.g. login field shared between log in and sign up modes).
- **Desktop widths**: form should remain centered and proportional from 1024px to 1920px.

## Resolved Questions

1. **Form mode switching**: 3-tab switcher at the top (Log in / Sign up / Anonymous) — no separate sign-up button or "or you can" divider.
2. **Fields**: Log in = login + password. Sign up = login + nick + password + confirm password. Anonymous = display name.
3. **Anonymous flow**: display name input shown when "Anonymous" tab is active.
4. **Styling**: original minimalistic design, user will review and request changes.

## Open Questions

None — all questions resolved.

## Notes

- Figma reference: https://www.figma.com/design/TiLhjkFAAarLpqg2VzLmeD/fasternfaster (page: "registration", node `1:2`)
- Figma structure: centered form with login/password inputs, log-in + anonymous options in a row, sign-up prompt below.
- Config files (package.json, Dockerfile, nginx.conf) are kept intact; only `src/` is rebuilt.
- This is the first page of a full frontend rebuild — lobbies page and lobby view will follow.
