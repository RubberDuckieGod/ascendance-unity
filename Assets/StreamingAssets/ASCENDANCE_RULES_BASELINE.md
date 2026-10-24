# Ascendance Unity rules baseline

This prototype follows the currently locked rules used by the 3.36.13 / v10.50 baseline:

- Hero attacks automatically hit. Hero Damage = Attack + modifiers - Enemy Armor (minimum 0).
- Enemy attacks roll 1d6. Enemy Damage = 1d6 + Enemy Attack + modifiers - Hero Armor (minimum 0).
- Heroes normally receive 3 Actions per Round.
- Movement is Location-based. A Hero moves only between connected, revealed Locations.
- Explore costs 1 Action and reveals a connected unrevealed Location without moving the Hero.
- Engagement prevents normal voluntary movement while a hostile Enemy shares the Hero's Location.
- Q001 vertical-slice objective: defeat the Goblin Captain, rescue at least 2 Villagers, keep Fire below 5.
- Garruk vertical-slice behavior includes Battle Roar and Bloodrage specifically to exercise the Hero-card modifier pipeline fixed in 3.36.11.

This Unity prototype intentionally keeps the game rules node/Location based while presenting them in a cinematic 3D environment.
