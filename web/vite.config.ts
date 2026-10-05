import { fileURLToPath } from 'node:url'
import { lingui } from '@lingui/vite-plugin'
import babel from '@rolldown/plugin-babel'
import { tanstackRouter } from '@tanstack/router-plugin/vite'
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vitest/config'

// Aspire injects the API address (WithReference) and the port to listen on.
// Prefer the API's http endpoint: the browser talks to Vite over http, and an https hop here would make the
// API mark the auth cookie Secure, which Safari then drops on http://localhost (sign-in loops back to /login).
const apiTarget =
  process.env.services__api__http__0 ?? process.env.services__api__https__0 ?? 'http://localhost:5276'

const src = (path: string) => fileURLToPath(new URL(`./src/${path}`, import.meta.url))

export default defineConfig({
  plugins: [
    tanstackRouter({
      target: 'react',
      routesDirectory: './src/app/routes',
      generatedRouteTree: './src/app/routeTree.gen.ts',
      autoCodeSplitting: true,
    }),
    react(),
    babel({ plugins: ['@lingui/babel-plugin-lingui-macro'] }),
    lingui(),
  ],
  resolve: {
    alias: {
      '@app': src('app'),
      '@features': src('features'),
      '@shared': src('shared'),
    },
  },
  server: {
    port: Number(process.env.PORT) || 5173,
    strictPort: true,
    // Same-origin API calls so the SameSite=Strict auth cookie works.
    proxy: {
      // xfwd passes the browser's scheme (X-Forwarded-Proto) so the API's cookie policy matches what the browser sees.
      '/api': { target: apiTarget, changeOrigin: true, secure: false, xfwd: true },
    },
  },
  test: {
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
  },
})
