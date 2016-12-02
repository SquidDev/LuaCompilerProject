(define-native byte)
(define-native char)
(define-native concat)
(define-native find)
(define-native format)
(define-native lower)
(define-native reverse)
(define-native rep)
(define-native replace)
(define-native sub)
(define-native upper)

(define-native #s)

(defun char-at (text pos) (char text pos pos))

(defun .. (&args) (concat args))

(defun split (text pattern limit)
  (let ((out '())
        (loop true)
        (start 1))
    (while loop
      (with (pos (find text pattern start))
        (if (or (= "nil" (type pos)) (and limit (>= (# out) limit)))
          (progn
            (set! loop false)
            (push-cdr! out (sub text start (#s text)))
            (set! start (+ (#s text) 1)))
          (progn
            (push-cdr! out (sub text start (- (car pos) 1)))
            (set! start (+ (cadr pos) 1))))))
    out))
