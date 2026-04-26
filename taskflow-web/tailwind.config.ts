import type { Config } from 'tailwindcss';
import forms from '@tailwindcss/forms';

export default {
  content: ['./index.html', './src/**/*.{ts,tsx}'],
  theme: {
    // ── SPACING — strict 8px grid ──────────────────────
    // Default Tailwind spacing is fine; we add named semantic tokens
    extend: {
      // ── COLORS ──────────────────────────────────────
      colors: {
        // Primary — Indigo (Atlassian Blue equivalent)
        primary: {
          50: '#eef2ff',
          100: '#e0e7ff',
          200: '#c7d2fe',
          300: '#a5b4fc',
          400: '#818cf8',
          500: '#6366f1',
          600: '#4f46e5',
          700: '#4338ca',
          800: '#3730a3',
          900: '#312e81',
          950: '#1e1b4b',
        },
        // Neutral — replaces Tailwind gray with warmer tone
        neutral: {
          0: '#ffffff',
          50: '#f8f9fa', // page bg
          100: '#f1f3f5', // sidebar / panel bg
          150: '#e9ecef', // subtle dividers
          200: '#dee2e6', // borders
          300: '#ced4da', // stronger borders
          400: '#adb5bd', // placeholder text
          500: '#868e96', // secondary text
          600: '#495057', // body text
          700: '#343a40', // headings
          800: '#212529', // primary text
          900: '#0d1117', // max contrast
        },
        // Semantic surfaces
        surface: {
          page: '#f4f5f7', // Atlassian's N20 equivalent — main bg
          card: '#ffffff', // content cards
          raised: '#ffffff', // modals, dropdowns
          sunken: '#f4f5f7', // inputs, code blocks
          overlay: 'rgba(9,30,66,0.54)', // modal backdrop — Atlassian standard
        },
        // Status colors (Atlassian-aligned)
        status: {
          'todo-bg': '#dfe1e6',
          'todo-text': '#42526e',
          'progress-bg': '#deebff',
          'progress-text': '#0747a6',
          'done-bg': '#e3fcef',
          'done-text': '#006644',
          'cancelled-bg': '#f4f5f7',
          'cancelled-text': '#6b778c',
          'high-bg': '#ffebe6',
          'high-text': '#bf2600',
          'medium-bg': '#fffae6',
          'medium-text': '#ff8b00',
          'low-bg': '#e3fcef',
          'low-text': '#006644',
          'none-bg': '#f4f5f7',
          'none-text': '#6b778c',
        },
      },

      // ── TYPOGRAPHY ──────────────────────────────────
      fontFamily: {
        // DM Sans — modern, clean like Atlassian's Charlie Display
        sans: ['"DM Sans"', 'system-ui', '-apple-system', 'sans-serif'],
        mono: ['"JetBrains Mono"', '"Fira Code"', 'monospace'],
      },
      fontSize: {
        // Atlassian density-aware scale
        '10': ['10px', { lineHeight: '14px', letterSpacing: '0.01em' }],
        '11': ['11px', { lineHeight: '16px' }],
        '12': ['12px', { lineHeight: '16px' }],
        '13': ['13px', { lineHeight: '20px' }], // primary body — most used
        '14': ['14px', { lineHeight: '20px' }], // large body
        '16': ['16px', { lineHeight: '24px' }], // subheadings
        '18': ['18px', { lineHeight: '28px' }],
        '20': ['20px', { lineHeight: '28px' }],
        '24': ['24px', { lineHeight: '32px', fontWeight: '600' }],
        '28': ['28px', { lineHeight: '36px', fontWeight: '600' }],
      },

      // ── CONTENT WIDTHS ───────────────────────────────
      maxWidth: {
        // Named content containers — use these, NOT arbitrary max-w values
        'content-sm': '640px', // forms, narrow pages
        'content-md': '768px', // single column content
        'content-lg': '1024px', // dashboard etc (most pages)
        'content-xl': '1280px', // wide tables, board
        'content-2xl': '1440px', // max — ultra-wide screens
        'content-full': '100%', // full-width (board, table in context)
      },

      // ── LAYOUT DIMENSIONS ───────────────────────────
      width: {
        sidebar: '220px', // main sidebar (Atlassian: 240px, we go tighter)
        'sidebar-collapsed': '48px', // icon-only collapsed state
        panel: '320px', // right detail panels
        'panel-lg': '520px', // task detail slide-over
      },
      height: {
        topbar: '48px', // was 64px — too tall. Atlassian uses 48px
        'row-sm': '32px', // table row compact
        'row-md': '40px', // table row default
        'row-lg': '48px', // table row comfortable
      },

      // ── BORDER RADIUS ───────────────────────────────
      // Atlassian uses very subtle rounding
      borderRadius: {
        none: '0',
        sm: '3px', // chips, badges
        DEFAULT: '4px', // inputs, buttons
        md: '6px', // cards, dropdowns
        lg: '8px', // modals, popovers
        xl: '12px', // reserved for hero elements only
        full: '9999px',
      },

      // ── SHADOWS ─────────────────────────────────────
      // Atlassian's elevation system
      boxShadow: {
        e100: '0 1px 1px rgba(9,30,66,0.25), 0 0 0 1px rgba(9,30,66,0.08)',
        e200: '0 3px 5px rgba(9,30,66,0.20), 0 0 1px rgba(9,30,66,0.31)',
        e300: '0 8px 16px -4px rgba(9,30,66,0.25), 0 0 1px rgba(9,30,66,0.31)',
        e400: '0 12px 24px -6px rgba(9,30,66,0.25), 0 0 1px rgba(9,30,66,0.31)',
        e500: '0 20px 32px -8px rgba(9,30,66,0.25), 0 0 1px rgba(9,30,66,0.31)',
        // Alias
        card: '0 1px 1px rgba(9,30,66,0.25), 0 0 0 1px rgba(9,30,66,0.08)',
        overlay: '0 8px 16px -4px rgba(9,30,66,0.25), 0 0 1px rgba(9,30,66,0.31)',
        modal: '0 20px 32px -8px rgba(9,30,66,0.25), 0 0 1px rgba(9,30,66,0.31)',
      },

      // ── ANIMATIONS ──────────────────────────────────
      keyframes: {
        'fade-up': {
          from: { opacity: '0', transform: 'translateY(4px)' },
          to: { opacity: '1', transform: 'translateY(0)' },
        },
        'fade-in': { from: { opacity: '0' }, to: { opacity: '1' } },
        'slide-right': { from: { transform: 'translateX(100%)' }, to: { transform: 'translateX(0)' } },
        'scale-in': {
          from: { opacity: '0', transform: 'scale(0.97)' },
          to: { opacity: '1', transform: 'scale(1)' },
        },
      },
      animation: {
        'fade-up': 'fade-up 0.15s ease-out',
        'fade-in': 'fade-in 0.12s ease-out',
        'slide-right': 'slide-right 0.25s cubic-bezier(0.16,1,0.3,1)',
        'scale-in': 'scale-in 0.15s ease-out',
      },
    },
  },
  plugins: [forms({ strategy: 'class' })],
} satisfies Config;
