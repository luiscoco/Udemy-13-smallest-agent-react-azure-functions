import { defineConfig } from 'vitest/config';
import react from '@vitejs/plugin-react';
export default defineConfig({ plugins: [react()],
 server: { proxy: Object.fromEntries(['/api'].map(path => [path, { target:'http://127.0.0.1:7073', changeOrigin:false }])) },
 test: { environment:'jsdom', setupFiles:['./tests/setup.ts'] }
});
