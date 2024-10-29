import { message } from "@/hooks/useMessage";

/**
 * @description hex color to rgb color
 * @param {String} str color value string
 * @returns {String} Returns the processed color value
 */
export function hexToRgb(str: string) {
  let hexs: any = "";
  let reg = /^#?[0-9A-Fa-f]{6}$/;
  if (!reg.test(str)) return message.warning("Enter wrong hex color value");
  str = str.replace("#", "");
  hexs = str.match(/../g);
  for (let i = 0; i < 3; i++) hexs[i] = parseInt(hexs[i], 16);
  return hexs;
}

/**
 * @description RGB color to Hex color
 * @param {*} r red
 * @param {*} g green
 * @param {*} b blue
 * @returns {String} Returns the processed color value
 */
export function rgbToHex(r: any, g: any, b: any) {
  let reg = /^\d{1,3}$/;
  if (!reg.test(r) || !reg.test(g) || !reg.test(b)) return message.warning("Enter wrong rgb color value");
  let hexs = [r.toString(16), g.toString(16), b.toString(16)];
  for (let i = 0; i < 3; i++) if (hexs[i].length == 1) hexs[i] = `0${hexs[i]}`;
  return `#${hexs.join("")}`;
}

/**
 * @description Darken the color value
 * @param {String} color color value string
 * @param {Number} level The degree of deepening, limited between 0-1
 * @returns {String} Returns the processed color value
 */
export function getDarkColor(color: string, level: number) {
  let reg = /^#?[0-9A-Fa-f]{6}$/;
  if (!reg.test(color)) return message.warning("Enter wrong hex color value");
  let rgb = hexToRgb(color);
  for (let i = 0; i < 3; i++) rgb[i] = Math.round(20.5 * level + rgb[i] * (1 - level));
  return rgbToHex(rgb[0], rgb[1], rgb[2]);
}

/**
 * @description Lighten the color value
 * @param {String} color color value string
 * @param {Number} level The degree of deepening, limited between 0-1
 * @returns {String} Returns the processed color value
 */
export function getLightColor(color: string, level: number) {
  let reg = /^#?[0-9A-Fa-f]{6}$/;
  if (!reg.test(color)) message.warning("Enter wrong hex color value");
  let rgb = hexToRgb(color);
  for (let i = 0; i < 3; i++) rgb[i] = Math.round(255 * level + rgb[i] * (1 - level));
  return rgbToHex(rgb[0], rgb[1], rgb[2]);
}
