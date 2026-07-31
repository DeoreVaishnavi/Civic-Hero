/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{js,ts,jsx,tsx}'],
  theme: {
    extend: {
      fontFamily: {
        sans: ['Inter', 'ui-sans-serif', 'system-ui', 'sans-serif'],
      },
      keyframes: {
        civicFloat: {
          '0%, 100%': { transform: 'translateY(0) rotateX(58deg) rotateZ(-6deg)' },
          '50%': { transform: 'translateY(-10px) rotateX(56deg) rotateZ(-4deg)' },
        },
        civicFloatDelayed: {
          '0%, 100%': { transform: 'translateY(0) rotate(-2deg)' },
          '50%': { transform: 'translateY(-12px) rotate(2deg)' },
        },
        civicOrbit: {
          '0%, 100%': { transform: 'translate(0, 0) rotate(-3deg)' },
          '50%': { transform: 'translate(14px, -10px) rotate(3deg)' },
        },
        civicScan: {
          '0%': { backgroundPosition: '-80% 0' },
          '100%': { backgroundPosition: '180% 0' },
        },
        showcaseProgress: {
          '0%': { width: '0%' },
          '100%': { width: '100%' },
        },
      },
      animation: {
        'civic-float': 'civicFloat 5s ease-in-out infinite',
        'civic-float-delayed': 'civicFloatDelayed 4.5s ease-in-out infinite 700ms',
        'civic-orbit': 'civicOrbit 5.8s ease-in-out infinite',
        'civic-scan': 'civicScan 3.8s linear infinite',
        'showcase-progress': 'showcaseProgress 6.5s linear forwards',
      },
    },
  },
  plugins: [],
};
