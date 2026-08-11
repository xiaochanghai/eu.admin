export function element(tag, attributes = {}, ...children) {
  const node = document.createElement(tag);
  for (const [name, value] of Object.entries(attributes)) {
    if (value === undefined || value === null || value === false) continue;
    if (name === "className") node.className = value;
    else if (name === "dataset") Object.assign(node.dataset, value);
    else if (name.startsWith("aria")) node.setAttribute(name.replace(/[A-Z]/g, letter => `-${letter.toLowerCase()}`), String(value));
    else if (name === "disabled" || name === "hidden") node[name] = Boolean(value);
    else node.setAttribute(name, String(value));
  }
  append(node, children);
  return node;
}

export function append(parent, children) {
  for (const child of children.flat(Infinity)) {
    if (child === undefined || child === null || child === false) continue;
    if (child instanceof Node) parent.append(child);
    else {
      const text = document.createElement("span");
      text.textContent = String(child);
      parent.append(text);
    }
  }
}

export function clear(node) {
  while (node.firstChild) node.removeChild(node.firstChild);
}

export function setText(node, value) {
  node.textContent = value ?? "";
}

export function option(value, label) {
  const node = document.createElement("option");
  node.value = value;
  node.textContent = label;
  return node;
}
