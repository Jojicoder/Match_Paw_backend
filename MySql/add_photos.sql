-- Add photo URLs to all demo animals by animal_id
UPDATE animals SET photo_url = 'https://images.unsplash.com/photo-1587300003388-59208cc962cb?w=400' WHERE animal_id = 1;
UPDATE animals SET photo_url = 'https://images.unsplash.com/photo-1514888286974-6c03e2ca1dba?w=400' WHERE animal_id = 2;
UPDATE animals SET photo_url = 'https://images.unsplash.com/photo-1589941013453-ec89f33b5e95?w=400' WHERE animal_id = 3;
UPDATE animals SET photo_url = 'https://images.unsplash.com/photo-1545249390-6bdfa286032f?w=400' WHERE animal_id = 4;
UPDATE animals SET photo_url = 'https://images.unsplash.com/photo-1505628346881-b72b27e84530?w=400' WHERE animal_id = 5;
UPDATE animals SET photo_url = 'https://images.unsplash.com/photo-1425082661705-1834bfd09dca?w=400' WHERE animal_id = 6;

-- Confirm results
SELECT animal_id, name, photo_url FROM animals ORDER BY animal_id;
