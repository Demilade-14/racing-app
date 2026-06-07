/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ["./src/**/*.{js,jsx,ts,tsx}"],
  theme: {
    extend: {
      colors: {
        f1red:    "#E8002D",
        f1dark:   "#0A0A0A",
        f1panel:  "#111111",
        f1card:   "#1A1A1A",
        f1border: "#2A2A2A",
        f1silver: "#C0C0C0",
        f1gold:   "#FFD700",
        f1purple: "#9B59B6",
        f1green:  "#27AE60",
        f1blue:   "#2980B9",
        f1orange: "#E67E22",
      },
      fontFamily: {
        f1: ['"Titillium Web"', '"Arial Narrow"', 'sans-serif'],
      },
      backgroundImage: {
        carbon: "repeating-linear-gradient(45deg,#1a1a1a 0px,#1a1a1a 2px,#111 2px,#111 8px),repeating-linear-gradient(-45deg,#1a1a1a 0px,#1a1a1a 2px,#111 2px,#111 8px)",
      },
      animation: {
        'slide-in-left':  'slideInLeft 0.4s ease-out',
        'slide-in-right': 'slideInRight 0.4s ease-out',
        'fade-in':        'fadeIn 0.3s ease-out',
        'count-up':       'countUp 1s ease-out',
        'pulse-slow':     'pulse 3s infinite',
        'scan-line':      'scanLine 2s linear infinite',
      },
      keyframes: {
        slideInLeft:  { from: { transform: 'translateX(-60px)', opacity: 0 }, to: { transform: 'translateX(0)', opacity: 1 } },
        slideInRight: { from: { transform: 'translateX(60px)',  opacity: 0 }, to: { transform: 'translateX(0)', opacity: 1 } },
        fadeIn:       { from: { opacity: 0 }, to: { opacity: 1 } },
        scanLine:     { from: { transform: 'translateY(-100%)' }, to: { transform: 'translateY(100vh)' } },
      },
      boxShadow: {
        'f1-glow':    '0 0 20px rgba(232,0,45,0.4)',
        'f1-glow-lg': '0 0 40px rgba(232,0,45,0.6)',
        'silver-glow':'0 0 15px rgba(192,192,192,0.3)',
      },
    },
  },
  plugins: [],
};
