import { defineConfig } from 'vite';
import { resolve } from 'path';

export default defineConfig({
  root: '.',
  base: '/',
  build: {
    outDir: '../dist',
    emptyOutDir: true,
    rollupOptions: {
      input: {
        main: resolve(__dirname, 'main.js')
      },
      output: {
        entryFileNames: 'js/[name].[hash].js',
        chunkFileNames: 'js/[name].[hash].js',
        assetFileNames: (assetInfo) => {
          const info = assetInfo.name.split('.');
          const ext = info[info.length - 1];
          if (/\.(css)$/.test(assetInfo.name)) {
            return 'css/[name].[hash].[ext]';
          }
          if (/\.(png|jpe?g|svg|gif|tiff|bmp|ico)$/i.test(assetInfo.name)) {
            return 'images/[name].[hash].[ext]';
          }
          if (/\.(woff2?|eot|ttf|otf)$/i.test(assetInfo.name)) {
            return 'fonts/[name].[hash].[ext]';
          }
          return 'assets/[name].[hash].[ext]';
        }
      }
    },
    sourcemap: true,
    minify: 'terser',
    terserOptions: {
      compress: {
        drop_console: true,
        drop_debugger: true
      }
    }
  },
  css: {
    preprocessorOptions: {
      scss: {
        additionalData: `@import "scss/variables";`
      }
    }
  },
  server: {
    port: 3000,
    open: false,
    cors: true,
    proxy: {
      '/api': {
        target: 'https://localhost:7055',
        changeOrigin: true,
        secure: false
      }
    }
  },
  optimizeDeps: {
    include: [
      'swiper',
      'simplebar',
      '@fancyapps/ui',
      'nouislider',
      'choices.js',
      'medium-zoom',
      'sweetalert2'
    ]
  }
});
