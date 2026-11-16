import { chromium, expect } from '@playwright/test';
import {mkdirSync} from 'node:fs';

const browser = await chromium.launch({channel:process.env.PLAYWRIGHT_CHANNEL || 'chrome', headless:true});
const page = await browser.newPage({viewport:{width:1440,height:1000}});
const errors=[];page.on('pageerror',error=>errors.push(error.message));
mkdirSync('verification',{recursive:true});
try {
 await page.goto('http://127.0.0.1:5400');
 await expect(page.getByText('API key not configured',{exact:true})).toBeVisible();
 await expect(page.getByLabel('What would you like to ask?')).toHaveValue('Hello world!');
 await page.getByRole('button',{name:/Run agent/}).click();
 await expect(page.getByRole('alert')).toContainText('Configure ANTHROPIC_API_KEY');
 await expect(page.locator('.state')).toHaveText('error');
 await page.getByRole('button',{name:'Dismiss',exact:true}).click();
 for (const [id,heading] of [['learn','Small enough to understand. Real enough to explore.'],['setup','One command starts both servers.'],['architecture','See how the pieces connect.'],['walkthrough','One request. Serverless boundaries.'],['lab','Start with a little curiosity.']]) {
  await page.locator(`.lesson-nav a[href="#${id}"]`).click();
  await expect(page.getByRole('heading',{name:heading,exact:true})).toBeVisible();
 }
 await page.locator('.lesson-nav a[href="#architecture"]').click();
 await expect(page.locator('.workflow-code-step')).toHaveCount(16);
 await expect(page.locator('.workflow-code-step .hljs-keyword').first()).toBeVisible();
 const svg = await page.request.get('http://127.0.0.1:5400/workflow.svg');
 const diagram = await svg.text();
 expect(diagram).toContain('fill="none"');expect(diagram).not.toContain('<style');
 await page.locator('.uml-card').screenshot({path:'verification/architecture-desktop.png'});
 await page.getByRole('button',{name:'Switch to dark mode'}).click();
 await expect(page.locator('html')).toHaveClass('dark');
 await page.reload();await expect(page.locator('html')).toHaveClass('dark');
 await page.getByRole('button',{name:'Switch to light mode'}).click();
 await page.setViewportSize({width:390,height:844});
 await page.locator('.lesson-nav a[href="#lab"]').click();
 expect(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth)).toBe(true);
 await page.screenshot({path:'verification/lab-mobile.png',fullPage:true});
 expect(errors).toEqual([]);
 console.log('Browser checks passed: five tabs, real missing-key stream, 16 colored UML steps, SVG, theme persistence and mobile overflow.');
} finally {await browser.close();}
