import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [tailwindcss(), react()],
  server: {
    proxy: {
      // Proxy API calls to the .NET backend during development
      '/api': {
        target: 'http://localhost:5192',
        changeOrigin: true,
      },
      '/uploads': {
        target: 'http://localhost:5192',
        changeOrigin: true,
      },
    },
  },
})
