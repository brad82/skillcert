import { tanstackRouter } from '@tanstack/router-plugin/vite'
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// Aspire injects the API address (WithReference) and the port to listen on.
const apiTarget =
  process.env.services__api__https__0 ?? process.env.services__api__http__0 ?? 'http://localhost:5276'

export default defineConfig({
  plugins: [tanstackRouter({ target: 'react', autoCodeSplitting: true }), react()],
  server: {
    port: Number(process.env.PORT) || 5173,
    strictPort: true,
    // Same-origin API calls so the SameSite=Strict auth cookie works.
    proxy: {
      '/api': { target: apiTarget, changeOrigin: true, secure: false },
    },
  },
})
