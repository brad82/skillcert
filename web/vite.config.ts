import { fileURLToPath } from 'node:url'
import { lingui } from '@lingui/vite-plugin'
import babel from '@rolldown/plugin-babel'
import { tanstackRouter } from '@tanstack/router-plugin/vite'
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vitest/config'

// Aspire injects the API address (WithReference) and the port to listen on.
const apiTarget =
  process.env.services__api__https__0 ?? process.env.services__api__http__0 ?? 'http://localhost:5276'

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
      '/api': { target: apiTarget, changeOrigin: true, secure: false },
    },
  },
  test: {
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
  },
})
