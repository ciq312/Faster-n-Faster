import { render } from "@testing-library/react";
import type { ReactElement } from "react";
import { Provider } from "react-redux";
import { MemoryRouter, Route, Routes, useLocation } from "react-router-dom";
import { setupStore } from "../app/store";
import { useMeQuery } from "../features/auth/authApi";
import type { GameHub } from "../features/connection/gameHub";
import BannerHost from "../shared/components/Banner/BannerHost";
import { fakeGameHub } from "./fakeGameHub";

type RenderPageOptions = {
  route?: string;
  probes?: string[];
  hub?: GameHub;
};

export function CurrentUser() {
  useMeQuery();
  return null;
}

function LocationProbe() {
  const { pathname, search } = useLocation();
  return <p>{pathname + search}</p>;
}

export function renderPage(
  page: ReactElement,
  { route = "/", probes = [], hub }: RenderPageOptions = {},
) {
  const store = setupStore({ hub: hub ?? fakeGameHub() });
  const [pagePath] = route.split("?");
  render(
    <Provider store={store}>
      <MemoryRouter initialEntries={[route]}>
        <BannerHost />
        <Routes>
          <Route path={pagePath} element={page} />
          {probes.map((path) => (
            <Route key={path} path={path} element={<LocationProbe />} />
          ))}
        </Routes>
      </MemoryRouter>
    </Provider>,
  );
  return { store };
}
