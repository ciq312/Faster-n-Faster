import { expect, test } from "@playwright/test";
import process from "node:process";

const login = process.env.E2E_LOGIN;
const password = process.env.E2E_PASSWORD;

if (!login || !password) {
  throw new Error(
    "Run e2e-test.sh to seed the player and set the credentials.",
  );
}

test("a player opens login form and log in ", async ({ page }) => {
  await page.goto("/");
  await page.getByRole("button", { name: "Log in", exact: true }).click();

  await page.getByLabel("login", { exact: true }).fill(login);
  await page.getByLabel("password", { exact: true }).fill(password);

  const form = page.locator("form");
  await form.getByRole("button", { name: "log in", exact: true }).click();

  await expect(page).toHaveURL(/\/lobbies/);

  await new Promise((resolve) => setTimeout(resolve, 5 * 1000));
});
