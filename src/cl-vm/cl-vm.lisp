(defpackage :lunula-vm
  (:use :common-lisp))

(in-package :lunula-vm)

(declaim (optimize (debug 3)))

;;;; UTILS ;;;;
(eval-when (:compile-toplevel :load-toplevel :execute)
  (defmacro print-vars (&rest vars)
    (let ((out (gensym)))
      (if (eql nil (first vars))
          '(progn)
        `(let ((,out *trace-output*))
           (when ,out
             (write-string "TRACE: " ,out)
             ,@(loop for var in vars
                     collect (cond ((or (keywordp var)
                                        (stringp var)) `(progn
                                                          (princ ,var ,out)
                                                          (princ " " ,out)))                             
                                   (t `(format ,out "~A=~S " ',var ,var))))
             (terpri ,out)
             (finish-output ,out)))))))

;;;; RUNTIME ;;;;


;; REGISTERS ;;
(defparameter *value*      nil "holds the return value of calls, or the function to call when making a call")
(defparameter *envt*       nil "holds the chain of environment frames")
(defparameter *cont*       nil "holds the chain of continuations")
(defparameter *template*   nil "holds the template of the current running procedure")
(defparameter *pc*         nil "program counter")
(defparameter *eval-stack* nil "holds parameters for function calls")


;; TOPLEVEL ;;
(defvar *toplevel* (make-hash-table))

(defun toplevel-get (symbol)
  (gethash symbol *toplevel*))

(defun toplevel-set (symbol value)
  (Setf (gethash symbol *toplevel*) value))

;; TEMPLATE ;;
(defclass template ()
  ((code :reader template-code)
   (literals :reader template-literals)))

(defun make-template (template-literal)
  (assert (eq 'template (first template-literal)))
  (assert (= 3 (length template-literal)))
  (let ((template (make-instance 'template)))
    (with-slots (literals code) template
      (setf literals (coerce (cadr template-literal) 'simple-vector))
      ;; make sure local template literals get loaded
      (loop for x from 0 below (length literals)
            do (symbol-macrolet ((literal (aref literals x)))
                 (when (and (consp literal)
                            (eq (first literal) 'template))
                   (setf literal (make-template literal)))))
      (setf code (compile-intermedate-code-to-byte-code (caddr template-literal))))
    template))


;; CLOSURE ;;
(defclass closure ()
  ((envt :reader closure-envt)
   (template :reader closure-template)))
   
(defun make-closure (envt template)
  (assert (or (null envt) (eq 'envt-frame (type-of envt))))
  (assert (eq 'template (type-of template)))
  (let ((closure (make-instance 'closure)))
    (with-slots ((cenvt envt) (ctemplate template)) closure
      (setf cenvt envt)
      (setf ctemplate template))
    closure))


;; This record saves the value of the environment, template, PC, and 
;; continuation registers, and any temporary values on the eval stack.
(defclass continuation ()
  ((envt)
   (pc)
   (template)
   (cont)
   (eval-stack)))
   
;; LEXICAL ENVIRONMENT ;;
(defclass envt-frame ()
  ((parent :reader envt-parent)
   (bindings :reader envt-bindings)))

(defun make-envt (parent number-of-slots)
  (let ((envt (make-instance 'envt-frame)))
    (with-slots ((eparent parent) (ebindings bindings)) envt
      (setf eparent parent)
      (setf ebindings (make-array number-of-slots)))
    envt))

;; OPERATIONS ;;
(defun op-toplevel-get ()
  "gets a toplevel binding, puts it in *value*"
  (setf *value* (toplevel-get *value*))
  (incf *pc*))

(defun op-toplevel-set ()
  ""
  (toplevel-set *value* (car *eval-stack*)))

(defun op-local-lookup (frame-index index)
  (let ((envt *envt*))
    (loop for x from 0 below frame-index
          do (setf envt (envt-parent envt)))
    (setf *value* (aref (envt-bindings envt) index)))
  (incf *pc*))

(defun op-fetch-literal (index)
  (let ((literal (aref (template-literals *template*) index)))
    (setf *value* literal)   
    (incf *pc*)
    literal))

(defun op-apply ()
  (cond ;; call a native function
        ((functionp *value*)
         (setf *value* (apply *value* *eval-stack*))
         (op-return))
        ;; call a closure
        ((eq 'closure (type-of *value*))
         (setf *envt* (closure-envt *value*))
         (setf *template* (closure-template *value*))
         (setf *pc* 0))
        (t (error "cannot apply ~A" *value*))))

(defun op-bind (n)
  "bind n number of items in the *eval-stack* to local variables"
  (setf *envt* (make-envt *envt* n))
  (loop for x from 0 below n
        for value = (pop *eval-stack*)
        do (setf (aref (envt-bindings *envt*) x) value))
  (incf *pc*))

(defun op-push ()
  (push *value* *eval-stack*)
  (incf *pc*))
 
(defun op-save-continuation (label-address)
  (let ((cont (make-instance 'continuation)))
    (with-slots (envt pc template cont eval-stack) cont
      (setf envt *envt*)
      (setf pc label-address)
      (setf cont *cont*)
      (setf template *template*)
      (setf eval-stack *eval-stack*))
    (setf *cont* cont))
  (setf *eval-stack* nil)
  (incf *pc*))

(defun op-make-closure ()
  "Template is in the *value* register.  Puts the closure
in the *value* register."
  (setf *value* (make-closure *envt* *value*))
  (incf *pc*))

(defun op-return ()
  (if *cont*
      (with-slots (envt pc template cont eval-stack) *cont*
        (setf *envt* envt)
        (setf *pc* pc)
        (setf *template* template)
        (setf *cont* cont)
        (setf *eval-stack* eval-stack))
    (throw 'lunula-end 0)))

(defun op-jmp ()
  )

(defun op-end ()
  (throw 'lunula-end 0))
  
(defparameter *opcode-dispatch* 
  (coerce (list #'op-save-continuation ; 0
                #'op-fetch-literal     ; 1
                #'op-push              ; 2
                #'op-apply             ; 3
                #'op-bind              ; 4
                #'op-make-closure      ; 5
                #'op-toplevel-get      ; 6
                #'op-toplevel-set      ; 7
                #'op-local-lookup      ; 8
                #'op-return            ; 9
                #'op-end               ;10
                #'op-jmp               ;11
                )
          'simple-vector))

(defparameter *opcode-index*
  '((save-continuation . 0)
    (fetch-literal     . 1)
    (push              . 2)
    (apply             . 3)
    (bind              . 4)
    (make-closure      . 5)
    (toplevel-get      . 6)
    (toplevel-set      . 7)
    (local-lookup      . 8)
    (return            . 9)
    (end               . 10)
    (jmp               . 11)))
    
(defun opcode-symbol (index)
  (car (find index *opcode-index* :key #'cdr)))

(defun run (closure)
  "Executes a closure object. This is the entry point to the byte-code interpreter"  
  (let ((*envt* (closure-envt closure))
        (*cont* nil)
        (*template* (closure-template closure))
        (*pc* 0)
        (*eval-stack* nil)
        (*value* nil))
    (catch 'lunula-end  
      (loop while T
            for byte-code = (aref (template-code *template*) *pc*)
            do (let* ((opcode (first byte-code))
                      (op (opcode-symbol opcode))
                      (args (cdr byte-code)))
                 (print-vars "regs:" *pc* *value* *eval-stack* *cont* *template* *envt*)
                 (print-vars "  op:" op args)
                 (terpri *trace-output*)
                 (apply (aref *opcode-dispatch* opcode)
                        args))))
    *value*))

;;;; COMPILER ;;;;

(defclass cenvt-frame ()
  ((parent)
   (symbols)))

(defun cenv-lookup (symbol)
  )

(defun compile-lambda (lambda cenv)
  (let ((vars (cadr lambda))
        (body (cddr lambda))
        (proc (make-instance 'template)))
    (with-slots (envt code literals) proc
      `(make-template ))))
            
;;;; TEST CODE ;;;;

(toplevel-set '+ #'+)
(toplevel-set '/ #'/)
(toplevel-set '* #'*)
(toplevel-set '- #'-)

(defun compile-intermedate-code-to-byte-code (code-list)
  (let ((labels (loop for code in code-list
                      for index from 0
                      when (symbolp code)
                      collect (cons code index)
                      and
                      do (decf index))))
    (coerce (loop for code in code-list
                  for index from 0
                  if (symbolp code)
                  do (decf index)
                  else
                  collect (let ((opcode (cdr (assoc (car code) *opcode-index*))))
                            (if (eq 'save-continuation (car code))
                                (list opcode (cdr (assoc (cadr code) labels)))
                              (cons opcode (cdr code)))))
            'simple-vector)))


(defparameter *test-closure*
  (make-closure 
   nil 
   (make-template 
    '(template (10 20 (template 
                       (+)
                       ((bind 2)
                        (local-lookup 0 0)
                        (push)
                        (local-lookup 0 1)
                        (push)
                        (fetch-literal 0)
                        (toplevel-get)
                        (apply))))
               ((fetch-literal 0)
                (push)
                (fetch-literal 1)
                (push)
                (fetch-literal 2)
                (make-closure)
                (apply))))))

;; (+ (+ 10 20)(+ 30 40))

(defparameter *test-closure-2*
  (make-closure 
   nil 
   (make-template 
    '(template (10 20 30 40 +)
               ((save-continuation label1)
                (fetch-literal 3)
                (push)
                (fetch-literal 2)
                (push)
                (fetch-literal 4)
                (toplevel-get)
                (apply)

                label1 
                (push)
                (save-continuation label2)
                (fetch-literal 1)
                (push)
                (fetch-literal 0)
                (push)
                (fetch-literal 4)
                (toplevel-get)
                (apply)

                label2
                (push)
                (fetch-literal 4)
                (toplevel-get)
                (apply))))))

(defparameter *test-closure-3*
  (make-closure
   nil
   (make-template
    '(template ()
               ()))))