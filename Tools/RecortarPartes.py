"""Recorta las partes del personaje desde el dibujo Assets/03_Sprites/Player/Player.png.

El dibujo trae dos versiones de Lumbra con las partes separadas: la de la izquierda (normal) y la de la derecha
(encorvada, para cuando está agotado). Cada una tiene cabeza, capa, brazo, brazo con farol y dos piernas.
Este script quita el fondo cuadriculado, guarda cada parte como PNG transparente en Player/Partes
y anota en Partes.json dónde se une cada una (cuello, hombros, cadera, mano).
Después, en Unity, "Lumbra > Construir nivel" arma al personaje con esas partes y crea sus animaciones.

Uso:  python Tools/RecortarPartes.py
"""
import json
import os
from collections import deque

import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
PLAYER = os.path.join(HERE, '..', 'Assets', '03_Sprites', 'Player')
OUT = os.path.join(PLAYER, 'Partes')


def load_parts(path):
    """Quita el fondo (gris claro unido a los bordes) y devuelve la imagen con transparencia y sus partes."""
    rgb = np.array(Image.open(path).convert('RGB')).astype(int)
    h, w, _ = rgb.shape
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    background_like = (abs(r - b) < 10) & (abs(r - g) < 10) & ((r + g + b) / 3 > 215)

    background = np.zeros((h, w), bool)
    queue = deque((y, x) for y in range(h) for x in (0, w - 1) if background_like[y, x])
    queue.extend((y, x) for x in range(w) for y in (0, h - 1) if background_like[y, x])
    for y, x in queue:
        background[y, x] = True
    while queue:
        y, x = queue.popleft()
        for ny, nx in ((y + 1, x), (y - 1, x), (y, x + 1), (y, x - 1)):
            if 0 <= ny < h and 0 <= nx < w and background_like[ny, nx] and not background[ny, nx]:
                background[ny, nx] = True
                queue.append((ny, nx))

    rgba = np.dstack([rgb, np.where(background, 0, 255)]).astype(np.uint8)
    labels = np.zeros((h, w), int)
    parts = []
    for y in range(h):
        for x in range(w):
            if not background[y, x] and labels[y, x] == 0:
                labels[y, x] = len(parts) + 1
                queue = deque([(y, x)])
                pixels = []
                while queue:
                    cy, cx = queue.popleft()
                    pixels.append((cy, cx))
                    for ny, nx in ((cy + 1, cx), (cy - 1, cx), (cy, cx + 1), (cy, cx - 1)):
                        if 0 <= ny < h and 0 <= nx < w and not background[ny, nx] and labels[ny, nx] == 0:
                            labels[ny, nx] = len(parts) + 1
                            queue.append((ny, nx))
                parts.append(np.array(pixels))
    return rgba, [p for p in parts if len(p) > 800]


def save_part(rgba, pixels, name, pivot_global, data):
    """Guarda una parte recortada y su punto de giro (en píxeles desde arriba a la izquierda de la parte)."""
    ys, xs = pixels[:, 0], pixels[:, 1]
    y0, x0 = ys.min(), xs.min()
    img = np.zeros((ys.max() - y0 + 1, xs.max() - x0 + 1, 4), np.uint8)
    img[ys - y0, xs - x0] = rgba[ys, xs]
    Image.fromarray(img).save(os.path.join(OUT, name + '.png'))
    data['parts'].append({'name': name, 'width': int(img.shape[1]), 'height': int(img.shape[0]),
                          'pivotX': float(pivot_global[0] - x0), 'pivotY': float(pivot_global[1] - y0)})


def top_center(pixels, rows):
    top = pixels[pixels[:, 0] < pixels[:, 0].min() + rows]
    return top[:, 1].mean(), top[:, 0].mean()


def lantern_pixels(rgba, lantern_arm):
    """Píxeles del farol (cuerpo, tapa y cuerda) dentro de la pieza brazo+farol.

    La piel es la mancha color crema más grande (brazo y mano); los brillos del farol son manchas chicas aparte.
    El farol es todo lo que se conecta con su cuerpo naranja sin tocar la piel y por debajo del borde de la mano.
    """
    ys, xs = lantern_arm[:, 0], lantern_arm[:, 1]
    color = rgba[ys, xs, :3].astype(int)
    r, g, b = color[:, 0], color[:, 1], color[:, 2]
    cream = (r > 200) & (g > 185) & (r - b < 70)
    orange = (r > 225) & (g > 150) & (g < 215) & (b < 120) & (r - b > 120)

    skin = largest_region({(y, x) for y, x, c in zip(ys, xs, cream) if c})
    lantern_top = ys[orange].min()
    bottom = {}  # en cada columna, el último píxel de piel encima del farol
    for y, x in skin:
        if y < lantern_top:
            bottom[x] = max(bottom.get(x, 0), y)
    # Dónde termina la mano en cada columna, mirando también las vecinas: así la cuerda y el borde
    # oscuro que pasan por encima de la mano se quedan en el brazo
    hand_edge = {x: max(bottom.get(x + d, 0) for d in range(-8, 9)) + 7 for x in set(xs)}

    body_left, body_right = xs[orange].min() - 2, xs[orange].max() + 2
    left, right = body_left - 12, body_right + 12  # a la altura del farol, también su borde oscuro
    allowed = {(y, x) for y, x in zip(ys, xs)
               if (y, x) not in skin and y > hand_edge.get(x, 0)
               and (body_left <= x <= body_right or (y >= lantern_top and left <= x <= right))}
    start = [(y, x) for y, x in zip(ys[orange], xs[orange])]
    return flood(start, allowed)


def largest_region(pixels):
    best = set()
    left = set(pixels)
    while left:
        region = flood([left.pop()], pixels)
        left -= region
        if len(region) > len(best):
            best = region
    return best


def flood(start, allowed):
    found = set(start)
    queue = deque(start)
    while queue:
        y, x = queue.popleft()
        for n in ((y + 1, x), (y - 1, x), (y, x + 1), (y, x - 1)):
            if n in allowed and n not in found:
                found.add(n)
                queue.append(n)
    return found


def cut_version(rgba, parts, left_side, prefix, data):
    """El dibujo mira a la derecha: la punta de la capucha cae hacia atrás (izquierda) y el farol va adelante."""
    side = [p for p in parts if (p[:, 1].mean() < rgba.shape[1] / 2) == left_side]
    side.sort(key=lambda p: p[:, 0].min())
    head, cloak = side[0], side[1]
    arms = sorted(side[2:4], key=lambda p: p[:, 1].mean())   # izquierda: brazo de atrás / derecha: brazo con farol
    leg = sorted(side[4:6], key=lambda p: p[:, 1].mean())[1]   # la de la bota hacia adelante

    # Cabeza: gira desde debajo del cráneo (al 75% del ancho, en su punto más bajo)
    hx = int(head[:, 1].min() + 0.75 * (head[:, 1].max() - head[:, 1].min()))
    save_part(rgba, head, prefix + 'Head', (hx, head[head[:, 1] == hx][:, 0].max()), data)

    # Capa: gira desde el cuello
    neck = top_center(cloak, 12)
    save_part(rgba, cloak, prefix + 'Cloak', neck, data)

    # Brazo de atrás: gira desde el hombro (arriba)
    save_part(rgba, arms[0], prefix + 'Arm', top_center(arms[0], 20), data)

    # Brazo con farol: el farol se separa para que cuelgue derecho, sin llevarse ningún pedazo de la mano
    lantern_arm = arms[1]
    lantern = lantern_pixels(rgba, lantern_arm)
    is_lantern = np.array([(y, x) in lantern for y, x in lantern_arm])
    arm, lantern = lantern_arm[~is_lantern], lantern_arm[is_lantern]
    shoulder = arm[arm[:, 1] < arm[:, 1].min() + 20]
    shoulder = (shoulder[:, 1].mean(), shoulder[:, 0].mean())
    hand = arm[arm[:, 1] > arm[:, 1].max() - 25]
    hand = (hand[:, 1].mean(), hand[:, 0].min())
    save_part(rgba, arm, prefix + 'LanternArm', shoulder, data)
    save_part(rgba, lantern, prefix + 'Lantern', top_center(lantern, 6), data)

    # Pierna: gira desde la cadera
    hip = top_center(leg, 15)
    save_part(rgba, leg, prefix + 'Leg', hip, data)

    # Medidas para armarlo (en píxeles del dibujo)
    data[prefix.strip('_')] = {
        'legLength': float(leg[:, 0].max() - hip[1]),
        'neckOffsetX': float(neck[0] - cloak[:, 1].mean()),
        'hemToNeck': float(cloak[:, 0].max() - neck[1]),
        'handX': float(hand[0] - shoulder[0]),
        'handY': float(hand[1] - shoulder[1]),
    }


def main():
    os.makedirs(OUT, exist_ok=True)
    rgba, parts = load_parts(os.path.join(PLAYER, 'Player.png'))
    data = {'parts': []}
    cut_version(rgba, parts, True, 'Normal_', data)
    cut_version(rgba, parts, False, 'Tired_', data)
    with open(os.path.join(OUT, 'Partes.json'), 'w', encoding='utf-8') as f:
        json.dump(data, f, indent=2)
    print('Partes guardadas en', os.path.abspath(OUT))


if __name__ == '__main__':
    main()
