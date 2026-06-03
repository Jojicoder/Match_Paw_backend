-- Drop in reverse FK dependency order
DROP TABLE IF EXISTS adoption_applications;
DROP TABLE IF EXISTS care_logs;
DROP TABLE IF EXISTS medical_records;
DROP TABLE IF EXISTS applicants;
DROP TABLE IF EXISTS animals;
DROP TABLE IF EXISTS users;

CREATE TABLE animals (
    animal_id INT AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    species VARCHAR(50) NOT NULL,
    breed VARCHAR(100),
    age INT,
    sex VARCHAR(20),
    intake_date DATE,
    adoption_status ENUM('Available', 'Pending', 'Adopted', 'Unavailable') DEFAULT 'Available',
    health_status VARCHAR(100),
    notes TEXT,
    photo_url VARCHAR(500),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE users (
    user_id INT AUTO_INCREMENT PRIMARY KEY,
    full_name VARCHAR(100) NOT NULL,
    email VARCHAR(150) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    role ENUM('Admin', 'Staff', 'Volunteer', 'AdoptionRepresentative') NOT NULL,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE medical_records (
    medical_record_id INT AUTO_INCREMENT PRIMARY KEY,
    animal_id INT NOT NULL,
    record_date DATE NOT NULL,
    treatment_type VARCHAR(100),
    description TEXT,
    veterinarian_name VARCHAR(100),
    next_appointment DATE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,

    FOREIGN KEY (animal_id) REFERENCES animals(animal_id)
);

CREATE TABLE care_logs (
    care_log_id INT AUTO_INCREMENT PRIMARY KEY,
    animal_id INT NOT NULL,
    user_id INT NOT NULL,
    log_date DATE NOT NULL,
    feeding_notes TEXT,
    cleaning_notes TEXT,
    behavior_notes TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,

    FOREIGN KEY (animal_id) REFERENCES animals(animal_id),
    FOREIGN KEY (user_id) REFERENCES users(user_id)
);

CREATE TABLE applicants (
    applicant_id INT AUTO_INCREMENT PRIMARY KEY,
    full_name VARCHAR(100) NOT NULL,
    email VARCHAR(150) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    phone VARCHAR(30),
    address VARCHAR(255),

    housing_type VARCHAR(50),
    has_pets BOOLEAN DEFAULT FALSE,
    has_children BOOLEAN DEFAULT FALSE,
    experience_with_pets VARCHAR(100),
    preferred_contact_method VARCHAR(50),

    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE adoption_applications (
    application_id INT AUTO_INCREMENT PRIMARY KEY,
    animal_id INT NOT NULL,
    applicant_id INT NOT NULL,
    application_date DATE NOT NULL,
    status ENUM('Pending', 'UnderReview', 'Approved', 'Rejected') DEFAULT 'Pending',

    reason TEXT,
    living_situation TEXT,
    work_schedule VARCHAR(100),
    has_yard BOOLEAN DEFAULT FALSE,
    landlord_approval BOOLEAN DEFAULT FALSE,
    other_pets_details TEXT,

    reviewed_by INT,
    reviewed_date DATE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,

    FOREIGN KEY (animal_id) REFERENCES animals(animal_id),
    FOREIGN KEY (applicant_id) REFERENCES applicants(applicant_id),
    FOREIGN KEY (reviewed_by) REFERENCES users(user_id)
);