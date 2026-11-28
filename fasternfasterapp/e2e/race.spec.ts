import { expect, test } from "@playwright/test";
import process from "node:process";

const login = process.env.E2E_LOGIN;
const password = process.env.E2E_PASSWORD;

if (!login || !password) {
  throw new Error(
    "Run e2e-test.sh to seed the player and set the credentials.",
  );
}

test("E2E full gameplay flow", async ({ browser }) => {
  test.setTimeout(90_000);

  const alicePage = await browser.newPage();
  await alicePage.goto("/");
  await alicePage.getByRole("button", { name: "Log in", exact: true }).click();

  await alicePage.getByLabel("login", { exact: true }).fill(login);
  await alicePage.getByLabel("password", { exact: true }).fill(password);

  const form = alicePage.locator("form");
  await form.getByRole("button", { name: "log in", exact: true }).click();

  await expect(alicePage).toHaveURL(/\/lobbies/);

  await alicePage
    .getByRole("button", { name: "create lobby", exact: true })
    .click();

  const createLobbyForm = alicePage.locator("form");

  await createLobbyForm.getByLabel("lobby name").fill("test lobby");
  await createLobbyForm
    .getByRole("button", { name: "create", exact: true })
    .click();

  await expect(
    alicePage.getByRole("heading", { name: "test lobby", exact: true }),
  ).toBeVisible();

  const bobPage = await browser.newPage();

  await bobPage.goto("/");
  await bobPage.getByLabel("display name").fill("bob");
  await bobPage.getByRole("button", { name: "play", exact: true }).click();

  const lobbyCard = bobPage
    .getByRole("region", { name: "Active lobbies" })
    .locator(".lobby-card")
    .filter({ has: bobPage.getByText("test lobby", { exact: true }) });
  await lobbyCard
    .getByRole("button", { name: "join lobby", exact: true })
    .click();

  await expect(bobPage).toHaveURL(alicePage.url());
  await expect(bobPage.getByText("Alice", { exact: true })).toBeVisible();
  await expect(alicePage.getByText("bob", { exact: true })).toBeVisible();

  await alicePage
    .getByRole("button", { name: "start race", exact: true })
    .click();

  const aliceInput = alicePage.locator(".typing-area__hidden-input");
  const bobInput = bobPage.locator(".typing-area__hidden-input");

  await expect(aliceInput).toBeEnabled({ timeout: 10_000 });
  await expect(bobInput).toBeEnabled({ timeout: 10_000 });

  const aliceChars = alicePage.locator(".typing-area__char");
  const bobChars = bobPage.locator(".typing-area__char");

  const passage = (await aliceChars.allTextContents()).join("");
  expect((await bobChars.allTextContents()).join("")).toBe(passage);

  const bobOnAlice = alicePage
    .locator("[data-progress-index]")
    .filter({ has: alicePage.getByText("bob", { exact: true }) });

  await bobInput.pressSequentially(passage.slice(0, 5), { delay: 120 });

  await expect(bobOnAlice).toHaveAttribute("data-progress-index", "4");

  const bobTyping = bobInput.pressSequentially(
    passage.slice(5, passage.length),
    { delay: 120 },
  );
  const aliceTyping = aliceInput.pressSequentially(passage, { delay: 140 });

  await Promise.all([bobTyping, aliceTyping]);

  const aliceRows = alicePage.locator(".race-results tbody tr");
  const bobRows = bobPage.locator(".race-results tbody tr");

  await expect(aliceRows.locator("td:first-child")).toHaveText(["1", "2"]);

  const finishOrder = await aliceRows
    .locator("td:nth-child(2)")
    .allTextContents();
  expect([...finishOrder].sort()).toEqual(["Alice", "bob"]);

  await expect(bobRows.locator("td:first-child")).toHaveText(["1", "2"]);
  await expect(bobRows.locator("td:nth-child(2)")).toHaveText(finishOrder);

  await new Promise((resolve) => setTimeout(resolve, 5 * 1000));
});
