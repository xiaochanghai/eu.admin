/**
 * @description Global theme config
 */
const globalTheme = {
  light: {
    // Customize global CSS variables
    "--hooks-colorBgContent": "#f5f5f5",
    "--hooks-colorLogoText": "#475768",
    "--hooks-colorTextRegular": "#606266",
    "--hooks-boxShadowAnalysis": "0 5px 20px 0 rgb(50 50 50 / 54%)",
    "--hooks-scrollbarThumb": "rgba(0, 0, 0, 0.1)",
    "--hooks-scrollbarThumb1": "rgba(0, 0, 0, 0.15)",

    // Customize login CSS variables
    "--hooks-colorBgLoginContainer": "#eeeeee",
    "--hooks-colorBgLoginMain": "rgb(255 255 255 / 80%)",
    "--hooks-boxShadowLoginForm": "0 2px 10px 2px rgb(0 0 0 / 10%)"
  },
  dark: {
    // Customize global CSS variables
    "--hooks-colorBgContent": "#0d0d0d",
    "--hooks-colorLogoText": "#f1f1f1",
    "--hooks-colorTextRegular": "#CFD3DC",
    "--hooks-boxShadowAnalysis": "0 3px 20px 0 rgb(255 255 255 / 35%)",
    "--hooks-scrollbarThumb1": "rgba(255, 255, 255, 0.3)",
    "--hooks-scrollbarThumb": "rgba(255, 255, 255, 0.4)",

    // Customize login CSS variables
    "--hooks-colorBgLoginContainer": "#191919",
    "--hooks-colorBgLoginMain": "rgb(0 0 0 / 80%)",
    "--hooks-boxShadowLoginForm": "0 2px 10px 2px rgb(255 255 255 / 12%)"
  }
};

export default globalTheme;
