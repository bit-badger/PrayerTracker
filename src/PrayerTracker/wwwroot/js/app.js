﻿/**
 * This file contains a library of common functions used throughout the PrayerTracker website, as well as specific
 * functions used on pages throughout the site.
 */
const PT = {

  /**
   * Open a window with help
   * @param {string} url The URL for the help page.
   */
  showHelp(url) {
    window.open(url, 'helpWindow', 'height=600px,width=450px,toolbar=0,menubar=0,scrollbars=1')
    return false
  },

  /**
   * Confirm, then submit a delete action
   * @param {string} action The URL for the action attribute of the delete form.
   * @param {string} prompt The localized prompt for confirmation.
   */
  confirmDelete(action, prompt) {
    if (confirm(prompt)) {
      let form = document.querySelector('#DeleteForm')
      form.setAttribute('action', action)
      form.submit()
    }
    return false
  },

  /**
   * Make fields required
   * @param {string[]|string} fields The field(s) to require
   */
  requireFields(fields) {
    (Array.isArray(fields) ? fields : [fields])
      .forEach(f => document.getElementById(f).required = true)
  },

  /**
   * Queue an action to occur when the DOM content is loaded
   * @param {Function} func The function to run once the DOM content is loaded
   */
  onLoad(func) {
    document.addEventListener('DOMContentLoaded', func)
  },

  /**
   * Validation that compares the values of 2 fields and fails if they do not match
   * @param {string} field1 The ID of the first field
   * @param {string} field2 The ID of the second fields (receives validate failure)
   * @param {string} errorMsg The validation error message to display
   * @returns {boolean} True if the fields match, false if not
   */
  compareValidation(field1, field2, errorMsg) {
    const field1Value = document.getElementById(field1).value
    const field2Element = document.getElementById(field2)
    if (field1Value === field2Element.value) {
      field2Element.setCustomValidity('')
      return true
    }
    field2Element.setCustomValidity(errorMsg)
    return false
  },

  /**
   * Show a div (adds CSS class)
   * @param {HTMLElement} div The div to be shown
   */
  showDiv(div) {
    if (div.className.indexOf(' pt-shown') === -1) {
      div.className += ' pt-shown'
    }
  },

  /**
   * Hide a div (removes CSS class)
   * @param {HTMLElement} div The div to be hidden
   */
  hideDiv(div) {
    div.className = div.className.replace(' pt-shown', '')
  },

  /**
   * Initialize CKEditor; this assumes that CKEditor will be running on a field with the ID of "Text"
   */
  initCKEditor() {
    ClassicEditor
      .create(document.querySelector('#text'))
      .catch(console.error)
  },

  /**
   * Scripts for pages served by the Church controller
   */
  church: {
    /**
     * Script for the church create / edit page
     */
    edit: {
      /**
       * If the interface box is checked, show and require the interface URL field (if not, well... don't)
       */
      checkInterface() {
        const div  = document.getElementById('divInterfaceAddress')
        const addr = document.getElementById('interfaceAddress')
        if (document.getElementById('hasInterface').checked) {
          PT.showDiv(div)
          addr.required = true
        }
        else {
          PT.hideDiv(div)
          addr.required = false
        }
      },
      /**
       * Set up the events for the page
       */
      onPageLoad() {
        PT.church.edit.checkInterface()
        document.getElementById('hasInterface')
          .addEventListener('click', PT.church.edit.checkInterface)
      }
    }
  },

  /**
   * Scripts for pages served by the Requests controller
   */
  requests: {
    /**
     * Script for the request view page
     */
    view: {
      /**
       * Prompt the user to remind them that they are about to e-mail their class
       * @param {string} confirmationPrompt The text to display to the user
       */
      promptBeforeEmail(confirmationPrompt) {
        return confirm(confirmationPrompt)
      },
    },
  },

  /**
   * Scripts for pages served by the SmallGroup controller
   */
  smallGroup: {
    /**
     * Script for the announcement page
     */
    announcement: {
      /**
       * Set up events and focus on the text field
       */
      onPageLoad() {
        PT.initCKEditor()
        const sendNo = document.getElementById('sendN')
        const catDiv = document.getElementById('divCategory')
        const catSel = document.getElementById('requestType')
        const addChk = document.getElementById('addToRequestList')
        if (sendNo !== 'undefined') {
          const addDiv = document.getElementById('divAddToList')
          sendNo.addEventListener('click', () => {
            PT.hideDiv(addDiv)
            PT.hideDiv(catDiv)
            catSel.required = false
            addChk.checked = false
          })
          document.getElementById('sendY').addEventListener('click', () => {
            PT.showDiv(addDiv)
          })
        }
        addChk.addEventListener('click', () => {
          if (addChk.checked) {
            PT.showDiv(catDiv)
            catSel.required = true
          } else {
            PT.hideDiv(catDiv)
            catSel.required = false
          }
        })
      }
    },

    /**
     * Script for the small group log on page
     */
    logOn: {
      /**
       * Determine which field should have the focus
       */
      onPageLoad() {
        const grp = document.getElementById('SmallGroupId')
        if (grp.options[grp.selectedIndex].value === '') {
          grp.focus()
        } else {
          document.getElementById('Password').focus()
        }
      }
    },

    /**
     * Script for the small group preferences page
     */
    preferences: {
      /**
       * Enable/disable the named or custom color inputs based on type.
       *
       * @param name The name of the color field set.
       */
      toggleType(name) {
        const isNamed = document.getElementById(`${name}Type_Name`)
        const named = document.getElementById(`${name}Color_Select`)
        const custom = document.getElementById(`${name}Color_Color`)
        if (isNamed.checked) {
          custom.disabled = true
          named.disabled = false
        } else {
          named.disabled = true
          custom.disabled = false
        }
      },

      /**
       * Show or hide the class password based on the visibility.
       */
      checkVisibility() {
        const divPw = document.getElementById('divClassPassword')
        if (document.getElementById('viz_Public').checked
            || document.getElementById('viz_Private').checked) {
          // Disable password
          PT.hideDiv(divPw)
        } else {
          PT.showDiv(divPw)
        }
      },

      /**
       * Bind the event handlers
       */
      onPageLoad() {
        ['Public', 'Private', 'Password'].map(typ => {
          document.getElementById(`viz_${typ}`).addEventListener('click',
            PT.smallGroup.preferences.checkVisibility)
        })
        PT.smallGroup.preferences.checkVisibility()
        ;['headingLine', 'headingText'].map(name => {
          document.getElementById(`${name}Type_Name`).addEventListener('click', () => {
            PT.smallGroup.preferences.toggleType(name)
          })
          document.getElementById(`${name}Type_RGB`).addEventListener('click', () => {
            PT.smallGroup.preferences.toggleType(name)
          })
          PT.smallGroup.preferences.toggleType(name)
        })
      },
    },
  },

  /**
   * Scripts for pages served by the User controller
   */
  user: {
    /**
     * Script for the user create / edit page
     */
    edit: {
      /**
       * Require password/confirmation for new users
       */
      onPageLoad(isNew) {
        if (isNew) {
          PT.requireFields(['password', 'passwordConfirm'])
        }
      }
    }
  },
}