# Audio Setup

El punto principal para cambiar sonidos ahora es:
`Assets/Project/Resources/Audio/ProjectAudioCatalog.asset`

Guia rapida:
- Musica global: grupo `Music` del catalogo.
- Sonidos del jugador: grupo `Player`.
- Alerta enemiga: grupo `Enemy`.
- Disparo, recarga y cargador vacio por defecto: grupo `Weapons`.
- Botones, paneles y creditos: grupos `UI` y `Credits`.

Reglas de prioridad:
- Si un componente tiene un clip asignado localmente, ese override sigue mandando.
- Si el campo local esta vacio, el componente usa el clip correspondiente del catalogo.
- Los botones y paneles ya no buscan sonidos por nombre dentro de una lista generica.

Puntos importantes del proyecto:
- `AudioManager` carga el catalogo automaticamente desde `Resources/Audio/ProjectAudioCatalog`.
- `PlayerAudioController`, `EnemyAudioController` y `WeaponScript` usan el catalogo como fallback explicito.
- Los cambios de audio compartido deben hacerse en el catalogo, no dentro de prefabs anidados, salvo que se quiera un override concreto para un objeto concreto.
