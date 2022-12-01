/** @type {import('tailwindcss').Config} */
module.exports = {
	mode: 'jit',
	darkMode: 'class',
	content: ['./**/*.{razor,html}'],
	theme: {
		fontSize: {
			'2xl': '1.4rem',
			'3xl': '1.875rem',
			'4xl': '2.25rem',
			'5xl': '3rem'
		},
		extend: {
			spacing: {
				'15': '10%',
				'20': '12%',
				'30': '15%'
			}
		},
	},
	plugins: [],
}
