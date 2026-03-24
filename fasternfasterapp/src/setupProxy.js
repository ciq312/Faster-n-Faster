const { createProxyMiddleware } = require("http-proxy-middleware");

module.exports = function (app) {
  app.use(
    "/gameHub",
    createProxyMiddleware({
      target: "http://localhost:8080",
      ws: true,
      changeOrigin: true,
    })
  );

  // Proxy all other API requests
  app.use(
    "/api",
    createProxyMiddleware({
      target: "http://localhost:8080",
      changeOrigin: true,
    })
  );
};
