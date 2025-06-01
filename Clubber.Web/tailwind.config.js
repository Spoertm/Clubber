/** @type {import('tailwindcss').Config} */
module.exports = {
	content: [
		'./Views/**/*.cshtml',
		'./wwwroot/js/**/*.js'
	],
	theme: {
		extend: {
			fontFamily: {
				'display': ['Inter', 'system-ui', 'sans-serif'],
				'body': ['Inter', 'system-ui', 'sans-serif'],
			},
			animation: {
				'fade-in': 'fadeIn 0.5s ease-in-out',
				'slide-up': 'slideUp 0.6s ease-out',
			},
			keyframes: {
				fadeIn: {
					'0%': { opacity: '0' },
					'100%': { opacity: '1' },
				},
				slideUp: {
					'0%': { opacity: '0', transform: 'translateY(30px)' },
					'100%': { opacity: '1', transform: 'translateY(0)' },
				},
			}
		}
	},
	plugins: [
		require('@tailwindcss/typography'),
	],
}
