USE matchpaw_db;

-- Delete child tables first because of foreign keys
DELETE FROM adoption_applications;
DELETE FROM care_logs;
DELETE FROM medical_records;
DELETE FROM applicants;
DELETE FROM users;
DELETE FROM animals;

ALTER TABLE adoption_applications AUTO_INCREMENT = 1;
ALTER TABLE care_logs AUTO_INCREMENT = 1;
ALTER TABLE medical_records AUTO_INCREMENT = 1;
ALTER TABLE applicants AUTO_INCREMENT = 1;
ALTER TABLE users AUTO_INCREMENT = 1;
ALTER TABLE animals AUTO_INCREMENT = 1;

-- =========================
-- 1. animals
-- =========================

INSERT INTO animals
(name, species, breed, age, sex, intake_date, adoption_status, health_status, notes, photo_url)
VALUES
('Buddy', 'Dog', 'Labrador Retriever', 3, 'Male', '2025-11-10', 'Available', 'Healthy', 'Friendly and energetic dog. Good with families.', 'https://images.unsplash.com/photo-1587300003388-59208cc962cb?w=400'),
('Luna', 'Cat', 'Domestic Shorthair', 2, 'Female', '2025-12-02', 'Available', 'Healthy', 'Calm and affectionate. Enjoys quiet spaces.', 'https://images.unsplash.com/photo-1514888286974-6c03e2ca1dba?w=400'),
('Max', 'Dog', 'German Shepherd', 5, 'Male', '2025-10-18', 'Pending', 'Under Treatment', 'Recovering from a minor leg injury.', 'https://images.unsplash.com/photo-1589941013453-ec89f33b5e95?w=400'),
('Bella', 'Cat', 'Siamese Mix', 4, 'Female', '2025-09-25', 'Adopted', 'Healthy', 'Very social and already adopted.', 'https://images.unsplash.com/photo-1545249390-6bdfa286032f?w=400'),
('Rocky', 'Dog', 'Beagle', 1, 'Male', '2026-01-08', 'Available', 'Healthy', 'Playful puppy. Needs basic training.', 'https://images.unsplash.com/photo-1505628346881-b72b27e84530?w=400'),
('Daisy', 'Rabbit', 'Holland Lop', 2, 'Female', '2026-01-20', 'Available', 'Healthy', 'Gentle rabbit. Good for experienced owners.', 'https://images.unsplash.com/photo-1425082661705-1834bfd09dca?w=400');

-- =========================
-- 2. users
-- password_hash values are temporary demo placeholders.
-- Real passwords should be hashed by the backend.
-- =========================

INSERT INTO users
(full_name, email, password_hash, role, is_active)
VALUES
('Sarah Johnson', 'admin@matchpaw.org', 'TEMP_HASH_ADMIN', 'Admin', TRUE),
('Michael Brown', 'staff@matchpaw.org', 'TEMP_HASH_STAFF', 'Staff', TRUE),
('Emily Davis', 'adoption@matchpaw.org', 'TEMP_HASH_ADOPTION', 'AdoptionRepresentative', TRUE),
('Tom Lee', 'volunteer@matchpaw.org', 'TEMP_HASH_VOLUNTEER', 'Volunteer', TRUE);

-- =========================
-- 3. applicants
-- New fields included:
-- housing_type, has_pets, has_children,
-- experience_with_pets, preferred_contact_method
-- =========================

INSERT INTO applicants
(full_name, email, password_hash, is_active, phone, address, housing_type, has_pets, has_children, experience_with_pets, preferred_contact_method)
VALUES
('John Smith', 'john.smith@example.com', 'TEMP_HASH_APPLICANT1', TRUE, '555-123-4567', '123 Main Street, Queens, NY', 'House', TRUE, TRUE, 'Experienced with dogs', 'Email'),
('Maria Garcia', 'maria.garcia@example.com', 'TEMP_HASH_APPLICANT2', TRUE, '555-234-5678', '45 Park Avenue, Brooklyn, NY', 'Apartment', FALSE, FALSE, 'First-time pet owner', 'Email'),
('Kevin Wilson', 'kevin.wilson@example.com', 'TEMP_HASH_APPLICANT3', TRUE, '555-345-6789', '88 River Road, Bronx, NY', 'House', TRUE, FALSE, 'Experienced with large dogs', 'Phone'),
('Aiko Tanaka', 'aiko.tanaka@example.com', 'TEMP_HASH_APPLICANT4', TRUE, '555-456-7890', '19 Maple Street, New York, NY', 'Apartment', FALSE, TRUE, 'Grew up with cats', 'Email'),
('Olivia Martinez', 'olivia.martinez@example.com', 'TEMP_HASH_APPLICANT5', TRUE, '555-567-8901', '200 Garden Avenue, Jersey City, NJ', 'Townhouse', TRUE, FALSE, 'Has experience with rabbits and cats', 'Phone'),

-- Demo accounts (password: Demo1234)
('Demo New User', 'demo.new@matchpaw.com', '$2b$12$/ZwckLhCX9tVTk3Ybc/gZe51e94nYBa20up9u1mAQpYdjXT/M71ly', TRUE, '555-000-0001', '1 Demo Street, New York, NY', 'Apartment', FALSE, FALSE, 'First-time pet owner', 'Email'),
('Demo Pending User', 'demo.pending@matchpaw.com', '$2b$12$qlJhgU4OKqaf2WHgMcuFXumIiRRVt2f0Taf3XlA1Cxq4nG.BeyI22', TRUE, '555-000-0002', '2 Demo Street, New York, NY', 'House', FALSE, FALSE, 'No prior experience', 'Email');

-- =========================
-- 4. medical_records
-- animal_id must match animals above
-- =========================

INSERT INTO medical_records
(animal_id, record_date, treatment_type, description, veterinarian_name, next_appointment)
VALUES
(1, '2025-11-12', 'Initial Checkup', 'General health check completed. No major issues found.', 'Dr. Miller', '2026-05-12'),
(2, '2025-12-04', 'Vaccination', 'Received core vaccination.', 'Dr. Smith', '2026-06-04'),
(3, '2025-10-20', 'Injury Treatment', 'Minor leg injury treated. Recovery is in progress.', 'Dr. Miller', '2026-02-20'),
(4, '2025-09-27', 'Spay Surgery', 'Spay surgery completed successfully.', 'Dr. Green', NULL),
(5, '2026-01-10', 'Initial Checkup', 'Healthy puppy. Recommended training and vaccination schedule.', 'Dr. Smith', '2026-03-10'),
(6, '2026-01-22', 'Dental Check', 'Basic dental check completed. No issues found.', 'Dr. Green', '2026-07-22');

-- =========================
-- 5. care_logs
-- user_id must match users above
-- =========================

INSERT INTO care_logs
(animal_id, user_id, log_date, feeding_notes, cleaning_notes, behavior_notes)
VALUES
(1, 2, '2026-02-01', 'Ate all food.', 'Kennel cleaned.', 'Very friendly and active.'),
(1, 4, '2026-02-02', 'Ate most food.', 'Water bowl replaced.', 'Enjoyed outdoor walk.'),
(2, 2, '2026-02-01', 'Ate slowly.', 'Litter box cleaned.', 'Calm and relaxed.'),
(3, 2, '2026-02-01', 'Ate all food.', 'Kennel cleaned.', 'Still limping slightly.'),
(5, 4, '2026-02-03', 'Ate all puppy food.', 'Kennel cleaned.', 'Very playful.'),
(6, 4, '2026-02-03', 'Ate hay and vegetables.', 'Cage cleaned.', 'Gentle and quiet.');

-- =========================
-- 6. adoption_applications
-- New fields included:
-- living_situation, work_schedule, has_yard,
-- landlord_approval, other_pets_details
-- reviewed_by can be NULL if not reviewed yet
-- =========================

INSERT INTO adoption_applications
(animal_id, applicant_id, application_date, status, reason, living_situation, work_schedule, has_yard, landlord_approval, other_pets_details, reviewed_by, reviewed_date)
VALUES
(1, 1, '2026-02-05', 'Pending',
 'I have experience with dogs and want a family-friendly pet.',
 'Lives in a house with a fenced backyard. Family members are comfortable with dogs.',
 'Full-time office job, but family members are home during the day.',
 TRUE,
 TRUE,
 'Currently has one older friendly dog.',
 NULL,
 NULL),

(2, 2, '2026-02-06', 'UnderReview',
 'I live in a quiet apartment and would like to adopt a calm cat.',
 'Lives alone in a quiet apartment. No other pets.',
 'Remote worker with flexible schedule.',
 FALSE,
 TRUE,
 'No current pets.',
 3,
 NULL),

(3, 3, '2026-02-07', 'Pending',
 'I am interested in adopting Max after he recovers.',
 'Lives in a house with enough space for a large dog.',
 'Works full-time but can walk the dog before and after work.',
 TRUE,
 TRUE,
 'Has one medium-sized dog that is friendly with other dogs.',
 NULL,
 NULL),

(4, 4, '2025-10-10', 'Approved',
 'I have owned cats before and can provide a stable home.',
 'Lives in an apartment with children. Family has experience with cats.',
 'Part-time work schedule, usually home in the afternoon.',
 FALSE,
 TRUE,
 'No current pets.',
 3,
 '2025-10-12'),

(5, 1, '2026-02-08', 'Rejected',
 'I would also like to adopt Rocky as a companion dog.',
 'Lives in a house, but already has a pending application for Buddy.',
 'Full-time office job.',
 TRUE,
 TRUE,
 'Currently has one older friendly dog.',
 3,
 '2026-02-09'),

(6, 5, '2026-02-10', 'Approved',
 'I have experience caring for rabbits and want to adopt Daisy.',
 'Lives in a townhouse with a dedicated indoor pet area.',
 'Works from home three days a week.',
 FALSE,
 TRUE,
 'Currently has one calm adult cat.',
 3,
 '2026-02-12'),

-- Demo pending account application (applicant_id 7 = demo.pending@matchpaw.com)
(1, 7, '2026-05-25', 'Pending',
 'I would love to give Buddy a loving home.',
 'Lives in a house with enough space for a dog.',
 'Works from home full-time.',
 TRUE,
 TRUE,
 'No current pets.',
 NULL,
 NULL);