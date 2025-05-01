// site.js - Global JavaScript functions for the application

// Scroll to element by ID with smooth scrolling
function scrollToElement(elementId) {
    const element = document.getElementById(elementId);
    if (element) {
        element.scrollIntoView({ 
            behavior: 'smooth', 
            block: 'start' 
        });
    } else {
        console.error(`Element with ID '${elementId}' not found`);
    }
}

// Function to prevent default anchor navigation and use smooth scrolling instead
document.addEventListener('DOMContentLoaded', () => {
    // Find all links with hash fragments that point to IDs on the same page
    document.querySelectorAll('a[href^="#"]').forEach(anchor => {
        anchor.addEventListener('click', function(e) {
            e.preventDefault();
            const targetId = this.getAttribute('href').substring(1);
            scrollToElement(targetId);
        });
    });
});

// Form Persistence Functions
function saveFormProgress(formId, answersJson) {
    try {
        localStorage.setItem(`form_progress_${formId}`, answersJson);
        return true;
    } catch (error) {
        console.error('Error saving form progress to localStorage:', error);
        return false;
    }
}

function getFormProgress(formId) {
    try {
        return localStorage.getItem(`form_progress_${formId}`);
    } catch (error) {
        console.error('Error retrieving form progress from localStorage:', error);
        return null;
    }
}

function clearFormProgress(formId) {
    try {
        localStorage.removeItem(`form_progress_${formId}`);
        return true;
    } catch (error) {
        console.error('Error clearing form progress from localStorage:', error);
        return false;
    }
}